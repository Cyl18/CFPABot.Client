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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace CFPABot.Client.Dialogs
{
    /// <summary>
    /// Interaction logic for AddSameMod.xaml
    /// </summary>
    public partial class AddSameModDialog
    {
        readonly string _repoPath;
        string[] mcVersions = Enum.GetValues<MCVersion>().Where(x => (int)x <= (int)MCVersion.v120fabric).Select(x => x.ToVersionString()).ToArray();

        public AddSameModDialog(string repoPath)
        {
            _repoPath = repoPath;
            InitializeComponent();

            var hs = new HashSet<string>();
            foreach (var file in Directory.GetFiles(_repoPath, "*.*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(_repoPath, file);
                var split = rel.Split("\\");
                if (split.Length < 7) continue;
                var path = $"{split[0]}\\{split[1]}\\{split[2]}\\{split[3]}\\{split[4]}";
                if (hs.Add(path))
                {
                    SourceDir.Items.Add(path);
                }
            }

            foreach (var mcVersion in mcVersions)
            {
                ToGameVersion.Items.Add(mcVersion);
            }
        }


    }
}
