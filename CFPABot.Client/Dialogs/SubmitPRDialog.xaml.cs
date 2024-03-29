using System;
using System.Collections.Generic;
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

namespace CFPABot.Client.Dialogs
{
    /// <summary>
    /// Interaction logic for SubmitPRDialog.xaml
    /// </summary>
    public partial class SubmitPRDialog
    {
        string _modNames;

        public SubmitPRDialog(string modNames)
        {
            InitializeComponent();
            _modNames = modNames;
            PRBody.Text += "\n\n提交自 CFPA-Client";
        }

        // 哈哈 写的难看就对啦！
        void 提交(object sender, RoutedEventArgs e)
        {
            PRTitle.Text = $"{_modNames} 翻译提交";
        }

        void 翻译(object sender, RoutedEventArgs e)
        {
            PRTitle.Text = $"{_modNames} 翻译更新";
        }

        void 修正(object sender, RoutedEventArgs e)
        {
            PRTitle.Text = $"{_modNames} 翻译修正";
        }
    }
}
