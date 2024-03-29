using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CFPABot.Client.Dialogs;
using GammaLibrary.Extensions;
using LibGit2Sharp;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Path = System.IO.Path;

namespace CFPABot.Client
{
    /// <summary>
    /// Interaction logic for AddModWindow.xaml
    /// </summary>
    public partial class AddModWindow
    {
        string dir;
        string[] mcVersions = Enum.GetValues<MCVersion>().Where(x => (int)x <= (int)MCVersion.v120fabric).Select(x => x.ToVersionString()).ToArray();

        public AddModWindow(string repoManagerWorkingDirectory)
        {
            InitializeComponent();
            dir = repoManagerWorkingDirectory;
            RefreshLang();

            contentDialogService = new ContentDialogService();
            contentDialogService.SetContentPresenter(RootContentDialog);
            foreach (var mcVersion in mcVersions)
            {
                MinecraftVersion.Items.Add(mcVersion);
            }
        }

        string cn, en;
        void RefreshLang()
        {
            LangInfo.Text = $"中文语言文件: {(cn.NotNullNorWhiteSpace() ? "✔" : "❌")} 英文语言文件: {(en.NotNullNorWhiteSpace()?"✔":"❌")}";
        }
        void AddModWindow_OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            
        }

        async void AddModWindow_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                foreach (var file in files)
                {
                    await ProcessFile(file);
                }
            }
            RefreshLang();
        }

        async Task ProcessFile(string path)
        {
            var extension = System.IO.Path.GetExtension(path);
            
            switch (extension.ToLower())
            {
                case ".jar":
                {
                    if (MinecraftVersion.SelectedIndex == -1)
                    {
                        ShowInfo("请先选择 Minecraft 版本");
                        return;
                    }

                    var modID = await GetModID(File.OpenRead(path));
                    ModDomain.Text = modID;
                    await using var fileStream = File.OpenRead(path);
                    var ens = GetModLangFilesFromStream(fileStream, LangType.EN,
                        MinecraftVersion.SelectedIndex == 1 ? LangFileType.Lang : LangFileType.Json);
                    en = await ens.First().Open().CreateStreamReader().ReadToEndAsync();
                    
                    break;
                }
                case ".lang":
                    MinecraftVersion.SelectedIndex = 1;
                    ProcessLangFile(path);
                    break;
                case ".json":
                    ProcessLangFile(path);
                    break;                
            }
        }

        void ProcessLangFile(string path)
        {
            var fileName = System.IO.Path.GetFileName(path).ToLowerInvariant();
            if (fileName.Contains("zh_cn"))
            {
                cn = File.ReadAllText(path);
            }
            else if (fileName.Contains("en_us"))
            {
                en = File.ReadAllText(path);
            }
            
        }

        public static async Task<string> GetModID(FileStream fs1, bool enforcedLang = true)
        {
            try
            {
                    await using var fs = fs1;

                    var modids = new ZipArchive(fs).Entries
                        .Where(a => a.FullName.StartsWith("assets"))
                        .Where(a => (!enforcedLang || a.FullName.Contains("lang")) && a.FullName.Contains("en_"))
                        .Select(a => a.FullName.Split("/")[1]).Distinct().Where(n => !n.IsNullOrWhiteSpace())
                        .Where(id => id != "minecraft" && id != "icon.png");

                    return modids.First();
                
            }
            catch (Exception e)
            {
                return $"未知";
            }
        }
        
        public static IEnumerable<ZipArchiveEntry> GetModLangFilesFromStream(Stream fs, LangType type, LangFileType fileType)
        {
            var files = new ZipArchive(fs).Entries
                .Where(f => f.FullName.Contains("lang") && f.FullName.Contains("assets") &&
                            f.FullName.Split('/').All(n => n != "minecraft") &&
                            type == LangType.EN
                    ? (f.Name.Equals("en_us.lang", StringComparison.OrdinalIgnoreCase) ||
                       f.Name.Equals("en_us.json", StringComparison.OrdinalIgnoreCase))
                    : (f.Name.Equals("zh_cn.lang", StringComparison.OrdinalIgnoreCase) ||
                       f.Name.Equals("zh_cn.json", StringComparison.OrdinalIgnoreCase))).ToArray();
            if (files.Length == 2 && files.Any(f => f.Name.EndsWith(".json")) && files.Any(f => f.Name.EndsWith(".lang"))) // storage drawers
            {
                files = fileType switch
                {
                    LangFileType.Lang => new[] {files.First(f => f.Name.EndsWith(".lang"))},
                    LangFileType.Json => new[] {files.First(f => f.Name.EndsWith(".json"))},
                    _ => throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null)
                };
            }
            return files;
        }
        
        string slug;
        HttpClient hc = new HttpClient();
        ContentDialogService contentDialogService;

        async void SlugVerify(object sender, RoutedEventArgs e)
        {
            slug = CurseForgeRadioButton.IsChecked == true ? Slug.Text : "modrinth-" + Slug.Text;
            var modName = await hc.GetAsync($"https://cfpa.cyan.cafe/api/Utils/ModName/{slug}");
            var result = await modName.Content.ReadAsStringAsync();
            if (modName.StatusCode == (HttpStatusCode)418 || !modName.IsSuccessStatusCode)
            {
                await Utils.ShowDialog($"{result}");
                return;
            }
            ShowInfo($"验证成功，模组名为 {result}");
            SlugPanel.IsEnabled = false;
        }

        void ShowInfo(string message)
        {
            var service = new SnackbarService();
            service.SetSnackbarPresenter(SnackbarPresenter);
            service.Show("提示", message, ControlAppearance.Info, new SymbolIcon(SymbolRegular.Info20), TimeSpan.FromSeconds(2));
        }

        async void AddMapping(object sender, RoutedEventArgs e)
        {
            var x = new AddMappingDialog();
            if (await contentDialogService.ShowAsync(x, CancellationToken.None) == ContentDialogResult.Primary)
            {
                var result = await hc.GetStringAsync($"https://cfpa.cyan.cafe/api/Utils/AddMapping/{x.CurseForgeProjectID.Text}");
                await Utils.ShowDialog(result);
            }
        }

        void Confirm(object sender, RoutedEventArgs e)
        {
            if (ModDomain.Text.IsNullOrWhiteSpace() || slug.IsNullOrWhiteSpace())
            {
                ShowInfo("请填写完整信息");
                return;
            }

            if (MinecraftVersion.SelectedIndex == -1)
            {
                ShowInfo("请选择 Minecraft 版本");
                return;
            }
            
            if (IsSubmitLangFile.IsChecked == true && (cn.IsNullOrWhiteSpace() || en.IsNullOrWhiteSpace()))
            {
                ShowInfo("请添加语言文件");
                return;
            }
            var mod = new
            {
                ModDomain = ModDomain.Text,
                MinecraftVersion = mcVersions[MinecraftVersion.SelectedIndex],
                CN = cn,
                EN = en
            };
            modPath = Path.Combine(dir, $"projects/{mod.MinecraftVersion}/assets/{slug}/{mod.ModDomain}/lang");
            
            Directory.CreateDirectory(modPath);
            var isLang = MinecraftVersion.SelectedIndex == 1;
            if (IsSubmitLangFile.IsChecked == true)
            {
                File.WriteAllText(Path.Combine(modPath, isLang ? "zh_cn.lang" : "zh_cn.json"), mod.CN, new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(modPath, isLang ? "en_us.lang" : "en_us.json"), mod.EN, new UTF8Encoding(false));
            }
            ShowInfo("添加成功");
            Close();
            
        }

        public string modPath { get; private set; }
    }
}
