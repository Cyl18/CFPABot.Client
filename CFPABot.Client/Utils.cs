using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFPABot.Client
{
    internal class Utils
    {
        public static async Task ShowDialog(string content, string title = "注意")
        {
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = content,

            };
            await uiMessageBox.ShowDialogAsync();
        }

        public static Task ShowDialog(Exception e) => ShowDialog("发生错误，如果是网络问题，请多尝试几次：\n" + e);
    }
}
