using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFPABot.Client
{
    class RepoAnalyzer
    {
        public string[] Analyze(string repo)
        {
            Directory.GetFiles(repo, "*.*", SearchOption.AllDirectories);
            throw new NotImplementedException();
        }
    }
}
