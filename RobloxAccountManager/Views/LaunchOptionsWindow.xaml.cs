using System.Windows;
using Wpf.Ui.Controls;

namespace RobloxAccountManager.Views
{
    public partial class LaunchOptionsWindow : FluentWindow
    {
        public string PlaceId { get; private set; } = string.Empty;
        public string JobId { get; private set; } = string.Empty;

        public LaunchOptionsWindow()
        {
            InitializeComponent();
            TxtPlaceId.Focus();
        }

        private void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            PlaceId = TxtPlaceId.Text.Trim();
            JobId = TxtJobId.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void BtnBrowser_Click(object sender, RoutedEventArgs e)
        {
            string currentPlaceId = TxtPlaceId.Text.Trim();
            var browser = new ServerBrowserWindow(currentPlaceId);
            browser.Owner = this;
            
            if (browser.ShowDialog() == true)
            {
                // Auto-fill result
                if (!string.IsNullOrEmpty(browser.SelectedPlaceId))
                    TxtPlaceId.Text = browser.SelectedPlaceId;
                    
                if (!string.IsNullOrEmpty(browser.SelectedJobId))
                    TxtJobId.Text = browser.SelectedJobId;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
