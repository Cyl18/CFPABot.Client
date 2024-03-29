using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CFPABot.Client
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            CFPABot.Client.App app = new CFPABot.Client.App();
            app.DispatcherUnhandledException += (sender, eventArgs) => MessageBox.Show(eventArgs.Exception.ToString());
            TaskScheduler.UnobservedTaskException +=
                (sender, eventArgs) => MessageBox.Show(eventArgs.Exception.ToString());
            app.InitializeComponent();
            app.Run();
        }
    }
}
