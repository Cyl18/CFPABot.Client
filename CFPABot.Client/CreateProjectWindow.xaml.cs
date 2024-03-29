using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using GammaLibrary.Extensions;
using Microsoft.VisualBasic;
using Octokit;

namespace CFPABot.Client
{
    /// <summary>
    /// Interaction logic for CreateProjectWindow.xaml
    /// </summary>
    public partial class CreateProjectWindow
    {
        RepoManager repoManager;
        Repository repo;
        GitHubClient gitHubClient = GitHub.GetClient();
        string githubToken = GitHub.GetToken();
        User user;
        string projectName;
        bool prMode;
        int prid;

        public CreateProjectWindow(string projectName)
        {
            InitializeComponent();
            repoManager = new RepoManager(projectName);
            this.projectName = projectName;
            prMode = false;
        }

        public CreateProjectWindow(int prid)
        {
            InitializeComponent();
            repoManager = new RepoManager("PR-"+ prid);
            this.projectName = "PR-" + prid;
            this.prid = prid;
            prMode = true;
        }

        async Task GetFork()
        {

            try
            {
                _updateAction("尝试直接获取 fork 库..");
                repo = await gitHubClient.Repository.Get(user.Login, "Minecraft-Mod-Language-Package");
                return;
            }
            catch (NotFoundException e)
            {
                _updateAction("获取失败，正在查找所有 fork 库..可能较慢..");
            }

            var forks = await gitHubClient.Repository.Forks.GetAll(Constants.RepoID);
            if (forks.FirstOrDefault(x => x.Owner.Login == user.Login) is { } fork)
            {
                repo = fork;
            }
            else
            {
                _updateAction("当前你没有 Fork, 正在创建一个新的 Fork");
                repo = await gitHubClient.Repository.Forks.Create(Constants.RepoID, new NewRepositoryFork());
                _updateAction("已经提交创建 Fork 请求，等待可用...");
                await Task.Delay(8000);
            }

            return;
        }
        void _updateAction(string t)
        {
            Logger.Text += t + "\n";
        }


        async void CreateProjectWindow_OnContentRendered(object? sender, EventArgs e)
        {
            try
            {
                user = await gitHubClient.User.Current();

                if (prMode)
                {
                    await ClonePR();
                    await SparseCheckout();
                }
                else
                {
                    await GetFork();
                    await Clone();
                }

                _updateAction("完成！");
                await Task.Delay(1000);
                DialogResult = true;
                Close();
            }
            catch (Exception exception)
            {
                await Utils.ShowDialog(exception);
                try
                {
                    Directory.Delete(repoManager.WorkingDirectory, true);
                }
                catch (Exception e1)
                {
                }
                DialogResult = false;
                Close();
            }

        }

        async Task SparseCheckout()
        {
            _updateAction("拉取文件中！");
            var hs = new HashSet<string>();
            foreach (var diff in await GitHub.Diff(prid))
            {
                var names = diff.To.Split('/');
                if (names.Length < 7) continue; // 超级硬编码
                if (names[0] != "projects") continue;
                if (names[5] != "lang") continue; // 只检查语言文件

                if (hs.Add($"{names[0]}/{names[1]}/{names[2]}/{names[3]}"))
                {
                    await repoManager.Run($"sparse-checkout add {names[0]}/{names[1]}/{names[2]}/{names[3]}");
                    _updateAction("拉取下一个文件中！");
                }
            }
        }

        async Task Clone()
        {
            _updateAction("Cloning Repo...");

            await repoManager.Clone(user.Login, repo.Name, user.Login, Settings.Instance.Email);
            await repoManager.Run($"sparse-checkout set --cone");
            await repoManager.Run("remote add upstream https://github.com/CFPAOrg/Minecraft-Mod-Language-Package.git");
            _updateAction("Fetching upstream...");
            var branchName = "CFPA-Client-" + projectName;
            await repoManager.Run("fetch upstream main");
            await repoManager.Run($"checkout -b {branchName} upstream/main");
            await repoManager.Run($"remote set-url origin {repo.CloneUrl}");
            _updateAction("Checking out...");
            _updateAction("Pushing...");
            await repoManager.Run($"push --set-upstream origin {branchName}");
            
            _updateAction("Done...");

        }

        async Task ClonePR()
        {
            var pr = await gitHubClient.PullRequest.Get(Constants.RepoID, prid);
            var user = pr.Head.Repository.Owner;
            _updateAction("Cloning Repo...");

            var branchName = pr.Head.Ref;
            await repoManager.Clone(user.Login, pr.Head.Repository.Name, this.user.Login, Settings.Instance.Email, branchName);
            await repoManager.Run($"sparse-checkout set --cone");
            await repoManager.Run($"checkout {branchName}");

            _updateAction("Done...");

        }
    }
}
