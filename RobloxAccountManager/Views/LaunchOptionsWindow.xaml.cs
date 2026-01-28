using System.Windows;
using Wpf.Ui.Controls;

namespace RobloxAccountManager.Views
{
    public partial class LaunchOptionsWindow : FluentWindow
    {
        public string PlaceId { get; private set; } = string.Empty;
        public string JobId { get; private set; } = string.Empty;
        public string AccessCode { get; private set; } = string.Empty;

        public LaunchOptionsWindow()
        {
            InitializeComponent();
            TxtPlaceId.Focus();
        }

        private void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            PlaceId = TxtPlaceId.Text.Trim();
            
            string currentJobInput = TxtJobId.Text.Trim();
            if (!string.IsNullOrEmpty(AccessCode) && currentJobInput == "(Private Server Access Code)")
            {
                JobId = "";
            }
            else
            {
                JobId = currentJobInput;
                AccessCode = ""; // User override
            }

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
                    
                if (!string.IsNullOrEmpty(browser.SelectedAccessCode))
                {
                    AccessCode = browser.SelectedAccessCode;
                    TxtJobId.Text = "(Private Server Access Code)";
                }
                else if (!string.IsNullOrEmpty(browser.SelectedJobId))
                {
                    AccessCode = "";
                    TxtJobId.Text = browser.SelectedJobId;
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
