using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Threading;
using CFPABot.Client.Dialogs;
using GammaLibrary.Extensions;
using LibGit2Sharp;
using Octokit;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;
using ListViewItem = Wpf.Ui.Controls.ListViewItem;
using Path = System.IO.Path;
using Repository = LibGit2Sharp.Repository;

namespace CFPABot.Client
{
    /// <summary>
    /// Interaction logic for ProjectWindow.xaml
    /// </summary>
    public partial class ProjectWindow
    {
        string _projectPath;
        RepoManager repoManager;
        GitHubClient gitHubClient;
        DispatcherTimer _timer;

        public ProjectWindow(string projectPath)
        {
            InitializeComponent();
            _projectPath = projectPath;
            projectName = Path.GetFileName(projectPath);
            repoManager = new RepoManager(projectName);
            gitHubClient = GitHub.GetClient();
            contentDialogService = new ContentDialogService();
            contentDialogService.SetContentPresenter(RootContentDialog);
            TitleBar.Title = "Project " + projectName;
        }

        async void OnTimer()
        {
            var changes = await GetChanges();
            Changes.Text = "未提交的文件:\n" + changes.Replace("\n", "\r\n");
            CommitAndPushButton.IsEnabled = changes.NotNullNorWhiteSpace();
        }

        Task<string> GetChanges()
        {
            return repoManager.RunWithReturnValue("status --porcelain");
        }

        void OpenProjectDirectory(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", _projectPath);
        }

        async void DeleteRepo(object sender, RoutedEventArgs e)
        {
            _timer.IsEnabled = false;
            await Process.Start(new ProcessStartInfo("git", "fsmonitor--daemon stop") {WorkingDirectory = _projectPath})
                .WaitForExitAsync();
            await Task.Delay(1000);
            try
            {
                Directory.Delete(_projectPath, true);
            }
            catch (Exception exception)
            {
                await Utils.ShowDialog("自动删除失败，请手动删除");
                Process.Start("explorer", Path.GetDirectoryName(_projectPath));
            }

            try
            {
                ((MainWindow) Owner).RefreshProjects();
            }
            catch (Exception exception)
            {
            }

            Close();
        }

        PullRequest? pr;
        string projectName;
        ContentDialogService contentDialogService;
        string branchName;

        public static async Task<PullRequest> GetPRFromHeadRef(string @ref)
        {
            try
            {
                var list = await GitHub.GetClient().PullRequest
                    .GetAllForRepository(Constants.RepoID, new PullRequestRequest() {State = ItemStateFilter.Open});
                list = list.Where(x => x.Head.Sha == @ref).ToList();
                return list.FirstOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        async void ProjectWindow_OnContentRendered(object? sender, EventArgs e)
        {
            MainPanel.IsEnabled = false;

            try
            {
                branchName = await repoManager.RunWithReturnValue("rev-parse --abbrev-ref HEAD");
                var head = await repoManager.RunWithReturnValue("rev-parse HEAD");
                repoManager.SetProxy();
                await repoManager.Run($"pull origin {branchName}");

                if (Settings.Instance.PRCache.TryGetValue(branchName, out var prid))
                {
                    pr = await gitHubClient.PullRequest.Get(Constants.RepoID, prid.ToInt());
                }
                else
                {
                    pr = await GetPRFromHeadRef(head);
                    if (pr != null)
                    {
                        Settings.Instance.PRCache[branchName] = pr.Number.ToString();
                    }
                }

                if (pr != null)
                {
                    PRHintTextBlock.Text = $"此项目绑定到 PR: {pr.Number}";
                    PRButton.Content = "打开 PR";
                }
                else
                {
                    PRHintTextBlock.Text = "";
                    PRButton.Content = "提交 PR";
                }

                OnTimer();
                RefreshMods();

                _timer = new DispatcherTimer(TimeSpan.FromSeconds(3), DispatcherPriority.Background,
                    (sender, args) => { OnTimer(); },
                    this.Dispatcher);

                MainPanel.IsEnabled = true;
            }
            catch (Exception exception)
            {
                await Utils.ShowDialog(exception.Message);
            }
        }

        async void CommitAndPush(object sender, RoutedEventArgs e)
        {
            var commitAndPushDialog = new CommitAndPushDialog();
            var res = await contentDialogService.ShowAsync(commitAndPushDialog, CancellationToken.None);
            if (res == ContentDialogResult.Primary)
            {
                try
                {
                    MainPanel.IsEnabled = false;
                    await SparseCheckoutLocal();
                    await repoManager.Run($"pull");
                    await repoManager.Run($"add -A");
                    await repoManager.Run(
                        $"commit -m \"{(commitAndPushDialog.CommitMessage.Text.Replace('\"', '*'))}\"");
                    await repoManager.Run($"push");
                }
                catch (Exception exception)
                {
                    await Utils.ShowDialog(exception);
                }

                MainPanel.IsEnabled = true;
            }
        }

        async void RefreshFiles(object sender, RoutedEventArgs e)
        {
            try
            {
                MainPanel.IsEnabled = false;

                await repoManager.Run($"pull");

                await SparseCheckoutLocal();
                await SparseCheckoutWeb();
                RefreshMods();
            }
            catch (Exception exception)
            {
                await Utils.ShowDialog(exception);
            }

            MainPanel.IsEnabled = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _timer.IsEnabled = false;
        }

        void RefreshMods()
        {
            Changes2.Items.Clear();
            var hs = new HashSet<string>();
            foreach (var file in Directory.GetFiles(repoManager.WorkingDirectory, "*.*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(repoManager.WorkingDirectory, file);
                if (relative.Contains("projects"))
                {
                    var names = relative.Split('\\');
                    if (names.Length < 5) continue;

                    if (hs.Add($"{names[1]}/{names[3]}/{names[4]}"))
                    {
                        var dir = $"{names[0]}\\{names[1]}\\{names[2]}\\{names[3]}\\{names[4]}";
                        var listViewItem = new ListViewItem()
                            {Content = $"{names[1]}/{names[3]}/{names[4]}"};
                        listViewItem.MouseDoubleClick += (sender, args) =>
                        {
                            var name = $"{repoManager.WorkingDirectory}\\{dir}";
                            Process.Start("explorer", name);
                        };
                        Changes2.Items.Add(listViewItem);
                    }
                }
            }
        }

        async Task SparseCheckoutSingle(string[] names)
        {
            if (names.Length < 5) return; // 超级硬编码
            if (names[0] != "projects") return;

            if (hs.Add($"{names[0]}/{names[1]}/{names[2]}/{names[3]}/{names[4]}"))
            {
                await repoManager.Run($"sparse-checkout add {names[0]}/{names[1]}/{names[2]}/{names[3]}/{names[4]}");
            }
        }

        private HashSet<string> hs = new HashSet<string>();

        async Task SparseCheckoutLocal()
        {
            foreach (var file in Directory.GetFiles(repoManager.WorkingDirectory, "*.*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(repoManager.WorkingDirectory, file);
                await SparseCheckoutSingle(rel.Split('\\'));
            }
        }

        async Task SparseCheckoutWeb()
        {
            try
            {
                if (pr != null)
                    foreach (var diff in await GitHub.Diff(pr.Number))
                    {
                        var names = diff.To.Split('/');
                        await SparseCheckoutSingle(names);
                    }
            }
            catch (Exception exception)
            {
                await Utils.ShowDialog(exception);
            }
        }

        async void AddMod(object sender, RoutedEventArgs e)
        {
            new AddModWindow(repoManager.WorkingDirectory).ShowDialog();
            await SparseCheckoutLocal();
            RefreshMods();
        }

        async void PRButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (pr == null)
            {
                var prDialog = new SubmitPRDialog(await GetModNames());
                if (await contentDialogService.ShowAsync(prDialog, CancellationToken.None) ==
                    ContentDialogResult.Primary)
                {
                    try
                    {
                        pr = await GitHub.GetClient().PullRequest.Create(Constants.RepoID,
                            new NewPullRequest(prDialog.PRTitle.Text,
                                    $"{(await gitHubClient.User.Current()).Login}:{branchName}", "main")
                                {Body = prDialog.PRBody.Text});
                        PRHintTextBlock.Text = $"此项目绑定到 PR: {pr.Number}";
                        PRButton.Content = "打开 PR";
                    }
                    catch (Exception exception)
                    {
                        await Utils.ShowDialog(exception.Message);
                    }
                }
            }
            else
            {
                Process.Start(
                    new ProcessStartInfo($"https://github.com/CFPAOrg/Minecraft-Mod-Language-Package/pull/{pr.Number}")
                        {UseShellExecute = true});
            }
        }


        HttpClient hc = new();

        async Task<string> GetModNames()
        {
            var list = new List<string>();
            var hs = new HashSet<string>();
            foreach (var file in Directory.GetFiles(repoManager.WorkingDirectory, "*.*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(repoManager.WorkingDirectory, file);
                if (relative.Contains("projects"))
                {
                    var names = relative.Split('\\');
                    if (names.Length < 5) continue;
                    try
                    {
                        if (hs.Add(names[3]))
                        {
                            var modName = await hc.GetAsync($"https://cfpa.cyan.cafe/api/Utils/ModName/{names[3]}");
                            var result = await modName.Content.ReadAsStringAsync();
                            if (modName.StatusCode == (HttpStatusCode) 418 || !modName.IsSuccessStatusCode) continue;
                            list.Add(result);
                        }
                    }
                    catch (Exception e)
                    {
                    }
                }
            }

            return list.Count == 0 ? "" : list.Connect(", ");
        }

        async void AddSameMod(object sender, RoutedEventArgs e)
        {
            var dialog = new AddSameModDialog(repoManager.WorkingDirectory);
            if (await contentDialogService.ShowAsync(dialog, CancellationToken.None) == ContentDialogResult.Primary)
            {
                if (dialog.ToGameVersion.SelectedIndex == -1 || dialog.SourceDir.SelectedIndex == -1)
                {
                    await Utils.ShowDialog("请选择版本");
                    return;
                }

                //projects/1.18/assets/shield-expansion/shieldexp
                var source = dialog.SourceDir.SelectedItem.ToString();
                var target = dialog.ToGameVersion.SelectedItem.ToString();
                var split = source.Split("\\");
                var targetDir = $"{split[0]}\\{target}\\{split[2]}\\{split[3]}\\{split[4]}";
                var path = Path.Combine(repoManager.WorkingDirectory, targetDir);
                Directory.CreateDirectory(path);
                File.WriteAllText(Path.Combine(path, "packer-policy.json"), $$"""
                                                                              [
                                                                                  {
                                                                                      "type": "indirect",
                                                                                      "source": "{{source.Replace('\\', '/')}}"
                                                                                  }
                                                                              ]
                                                                              """);
                await SparseCheckoutLocal();
                RefreshMods();
            }
        }
    }
}