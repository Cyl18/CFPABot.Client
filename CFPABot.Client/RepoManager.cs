using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CFPABot.Client
{
    public sealed class RepoManager
    {
        string _token;
        public string WorkingDirectory { get; }

        public RepoManager(string repoName)
        {
            WorkingDirectory = Path.Combine(Settings.Instance.StorageLocation, "projects", repoName);
            _token = GitHub.GetToken();
            Directory.CreateDirectory(WorkingDirectory);

        }

        public async Task Clone(string repoOwner, string repoName, string? userName = null, string? userEmail = null, string? branch = null)
        {
            if (Settings.Instance.UseProxy)
            {
                Run($"config http.proxy http://{Settings.Instance.Proxy}");
            }
            await Run($"clone {(branch == null ? "" : $"-b {branch}")} https://x-access-token:{_token}@github.com/{repoOwner}/{repoName}.git --filter=blob:none --no-checkout --depth 1 --sparse .");
            if (userEmail != null)
            {
                await Run($"config user.name \"{userName}\"");
                await Run($"config user.email \"{userEmail}\"");
            }

            
        }


        public void SetProxy()
        {
            if (Settings.Instance.UseProxy)
            {
                Run($"config http.proxy http://{Settings.Instance.Proxy}");
            }
        }
        // public void Commit(string message)
        // {
        //     Run("commit -m \"" +
        //         $"{message.Replace('\"', '*')}" +
        //         "\"");
        // }

        public async Task Run(string args)
        {
            var process = Process.Start(new ProcessStartInfo("git", args) { RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = WorkingDirectory, CreateNoWindow = true});
            var stdout = "";
            var stderr = "";
            process.OutputDataReceived += (sender, eventArgs) => { stdout += eventArgs.Data; };
            process.ErrorDataReceived += (sender, eventArgs) => { stderr += eventArgs.Data; };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                // haha
                // https://github.com/Cyl18/CFPABot/issues/3
                // maybe 
                await Utils.ShowDialog(stderr);
                throw new Exception($"git.exe with args `{Regex.Replace(args, "gh[sp]_[0-9a-zA-Z]{36}", "******")}` exited with {process.ExitCode}.");
            }

        }

        public async Task<string> RunWithReturnValue(string args)
        {
            var process = Process.Start(new ProcessStartInfo("git", args) { RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = WorkingDirectory, CreateNoWindow = true });
            var stdout = "";
            var stderr = "";
            process.OutputDataReceived += (sender, eventArgs) => { stdout += eventArgs.Data; };
            process.ErrorDataReceived += (sender, eventArgs) => { stderr += eventArgs.Data; };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                // haha
                // https://github.com/Cyl18/CFPABot/issues/3
                // maybe 
                await Utils.ShowDialog(stderr);
                throw new Exception($"git.exe with args `{Regex.Replace(args, "gh[sp]_[0-9a-zA-Z]{36}", "******")}` exited with {process.ExitCode}.");
            }

            return stdout;

        }

    }
}
