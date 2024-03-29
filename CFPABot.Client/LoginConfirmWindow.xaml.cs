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

namespace CFPABot.Client
{
    /// <summary>
    /// Interaction logic for LoginConfirmWindow.xaml
    /// </summary>
    public partial class LoginConfirmWindow
    {
        public LoginConfirmWindow(string[] emails)
        {
            InitializeComponent();
            foreach (var email in emails)
            {
                EmailComboBox.Items.Add(email);
            }
        }

        void Confirm(object sender, RoutedEventArgs e)
        {
            if (EmailComboBox.SelectedItem is string s)
            {
                Settings.Instance.Email = s;
                Settings.Save();
                DialogResult = true;
                Close();
            }
            
        }
    }
}
