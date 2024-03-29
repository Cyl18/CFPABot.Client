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
using System.Windows.Shapes;
using Wpf.Ui.Appearance;

namespace CFPABot.Client
{
    /// <summary>
    /// Interaction logic for ProxySettings.xaml
    /// </summary>
    public partial class ProxySettings
    {
        public ProxySettings()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(this);
        }

        void ProxySettings_OnContentRendered(object? sender, EventArgs e)
        {
            ProxyBox.Text = Settings.Instance.Proxy;
            UseProxyRadio.IsChecked = Settings.Instance.UseProxy;
        }

        void Save(object sender, RoutedEventArgs e)
        {
            Settings.Instance.Proxy = ProxyBox.Text;
            Settings.Instance.UseProxy = UseProxyRadio.IsChecked!.Value;
            Settings.Save();
            Close();
        }
    }
}
