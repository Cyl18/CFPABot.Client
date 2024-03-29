using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DiffPatch;
using DiffPatch.Data;
using GammaLibrary.Extensions;
using Octokit;
using Octokit.Caching;
using Octokit.Internal;

namespace CFPABot.Client
{
    internal class GitHub
    {
        static HttpClient hc = new HttpClient();
        public static GitHubClient GetClient()
        {
            var connection = new Connection(
                new ProductHeaderValue("Cyl18"),
                new HttpClientAdapter(() => new HttpClientHandler
                {
                    Proxy = new WebProxy(Settings.Instance.Proxy),
                    UseProxy = Settings.Instance.UseProxy
                }));
            connection.Credentials = new Credentials(GetToken());
            
            return new GitHubClient(connection);


        }

        public static async Task<FileDiff[]> Diff(int id)
            => DiffParserHelper.Parse((await hc.GetStringAsync($"https://cfpa.cyan.cafe/api/Utils/Diff/{id}"))
                // workaround https://github.com/CFPAOrg/Minecraft-Mod-Language-Package/pull/1924
                .Split("\n").Where(line => !line.StartsWith("rename ") && !line.StartsWith("similarity index ")).Connect("\n")
            ).ToArray();

        public static string GetToken()
        {
            return Settings.Instance.Token;
        }
    }

}
