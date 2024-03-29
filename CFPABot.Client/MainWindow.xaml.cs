using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using GammaLibrary.Extensions;
using LibGit2Sharp;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using static System.Net.Mime.MediaTypeNames;
using Path = System.IO.Path;

namespace CFPABot.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        public MainWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(this);
            if (Settings.Instance.StorageLocationEnum == StorageLocation.None)
            {
                if (new StorageLocationWindow().ShowDialog() != true)
                {
                    Close();
                }
            }

        }



        async void MainWindow_OnInitialized(object? sender, EventArgs e)
        {
            
        }

        public void RefreshProjects()
        {
            Projects.Children.Clear();
            foreach (var projectName in GetProjectNames())
            {
                Projects.Children.Add(new ProjectItem(projectName));
            }
        }

        List<string> GetProjectNames()
        {
            return Directory.GetDirectories(Path.Combine(Settings.Instance.StorageLocation, "projects"))
                .Where(x => Directory.Exists(Path.Combine(x, ".git"))).ToList();
        }

        async void NewProject(object sender, RoutedEventArgs e)
        {
            var text = ProjectNameBox.Text;
            if (text.IsNullOrWhiteSpace())
            {
                await Utils.ShowDialog("项目名不能为空");
                return;
            }

            new CreateProjectWindow(text.Trim()).ShowDialog();
            RefreshProjects();

        }

        void OpenProxySetting(object sender, RoutedEventArgs e)
        {
            new ProxySettings().ShowDialog();

        }

        async void ImportPR(object sender, RoutedEventArgs e)
        {
            var text = PRID.Value;
            if (text == null || text == 0)
            {
                await Utils.ShowDialog("PRID 有误");
                return;
            }
            new CreateProjectWindow((int)text).ShowDialog();
            RefreshProjects();


        }

        async Task CheckLogin()
        {
            try
            {
                MainPanel.IsEnabled = false;
                if (Settings.Instance.Token.IsNullOrWhiteSpace() || (await GitHub.GetClient().Repository.Get(Constants.RepoID)) == null)
                {
                    goto notLogin;
                }

                LoginStatusText.Text = "以 "+(await GitHub.GetClient().User.Current()).Login + " 的身份登入";
                MainPanel.IsEnabled = true;
            }
            catch (Exception exception)
            {
                await Utils.ShowDialog(exception.Message);
                goto notLogin;
            }
            return;
            notLogin:
            LoginStatusText.Text = "没有登录";
        }
        async void MainWindow_OnContentRendered(object? sender, EventArgs e)
        {
            try
            {
                Process.Start("git");
            }
            catch (Exception)
            {
                await Utils.ShowDialog("未检测到 git，点击确定后将打开浏览器，请手动安装。");
                Process.Start(
                    new ProcessStartInfo(
                            "https://mirrors.huaweicloud.com/git-for-windows/v2.44.0.windows.1/Git-2.44.0-64-bit.exe")
                        { UseShellExecute = true });
                Close();
            }

            RefreshProjects();
            await CheckLogin();

        }

        async void Login(object sender, RoutedEventArgs e1)
        {
            try
            {

                var hc = new HttpClient(new HttpClientHandler
                {
                    Proxy = new WebProxy(Settings.Instance.Proxy),
                    UseProxy = Settings.Instance.UseProxy
                });

                hc.DefaultRequestHeaders.Accept.Clear();
                hc.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var t = await Task.Run(() =>
                {
                    var auth = A125Interop.Auth();
                    return auth;
                });
                var p = await hc.PostAsync("https://github.com/login/oauth/access_token", new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", "73bb69a7c83d167c3cae"),
                    new KeyValuePair<string, string>("code", t),
                    new KeyValuePair<string, string>("client_secret", "147747460b26c952eb406dbff80d5244c2fdb220"),

                }));
                var json = await p.Content.ReadAsStream().CreateStreamReader().ReadToEndAsync();
                var clientAccessToken = JsonDocument.Parse(json).RootElement.GetProperty("access_token").GetString()!;
                Settings.Instance.Token = clientAccessToken;
                try
                {
                    var client = GitHub.GetClient();
                    await client.Repository.Get(Constants.RepoID);
                    new LoginConfirmWindow(await GetEmails()).ShowDialog();
                }
                catch (Exception e)
                {
                    await Utils.ShowDialog($"验证错误 {e.Message}");
                }

                await CheckLogin();
            }
            catch (Exception e)
            {
                await Utils.ShowDialog(e);
            }
        }

        public static async Task<string[]> GetEmails()
        {
            var client = GitHub.GetClient();
            try
            {
                var user = await client.User.Current().ConfigureAwait(false);
                var aEmail = $"{user.Id}+{user.Login}@users.noreply.github.com";
                try
                {
                    var readOnlyList = await client.User.Email.GetAll();
                    return readOnlyList.Select(x => x.Email).Append(aEmail).Distinct().ToArray();
                }
                catch (Exception e)
                {
                    return new[] { aEmail };
                }
            }
            catch (Exception e)
            {
                return new[] { "cyl18a+error@gmail.com" };
            }
        }
    }
}