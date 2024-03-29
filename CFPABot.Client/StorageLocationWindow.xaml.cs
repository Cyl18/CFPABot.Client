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
    /// Interaction logic for StorageLocationWindow.xaml
    /// </summary>
    public partial class StorageLocationWindow
    {
        public StorageLocationWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(this);
        }

        void Confirm(object sender, RoutedEventArgs e)
        {
            Settings.Instance.StorageLocationEnum =
                AppdataButton.IsChecked == true ? StorageLocation.Appdata : StorageLocation.CurrentDirectory;
            Settings.Save();
            DialogResult = true;
            Close();
        }
    }
}
