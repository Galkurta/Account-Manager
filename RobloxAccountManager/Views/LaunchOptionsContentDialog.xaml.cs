using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace RobloxAccountManager.Views
{
    public partial class LaunchOptionsContentDialog : ContentDialog
    {
        public string PlaceId { get; private set; } = string.Empty;
        public string JobId { get; private set; } = string.Empty;

        public string AccessCode { get; private set; } = string.Empty;

        public bool Launched { get; private set; } = false;

        public LaunchOptionsContentDialog(ContentDialogHost presenter) : base(presenter)
        {
            InitializeComponent();
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
            
            Launched = true;
            Hide(); 
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Launched = false;
            Hide();
        }

        private void BtnBrowser_Click(object sender, RoutedEventArgs e)
        {
            string currentPlaceId = TxtPlaceId.Text.Trim();
            
            var browserView = new ServerBrowserView();
            browserView.Initialize(currentPlaceId);
            browserView.ServerSelected += (placeId, jobId, accessCode) =>
            {
                TxtPlaceId.Text = placeId;
                
                if (!string.IsNullOrEmpty(accessCode))
                {
                     AccessCode = accessCode;
                     TxtJobId.Text = "(Private Server Access Code)";
                }
                else
                {
                     AccessCode = "";
                     TxtJobId.Text = jobId;
                }
                
                // Switch back
                SwitchToConfig();
            };
            
            browserView.BrowserCanceled += () =>
            {
                SwitchToConfig();
            };

            BrowserContainer.Content = browserView;
            
            // Switch View
            ConfigPanel.Visibility = Visibility.Collapsed;
            BrowserPanel.Visibility = Visibility.Visible;
            this.Width = 950; // Expand for browser
            this.Height = 600; 
        }

        private void SwitchToConfig()
        {
            BrowserPanel.Visibility = Visibility.Collapsed;
            ConfigPanel.Visibility = Visibility.Visible;
            this.Width = 500;
            this.Height = double.NaN; // Auto
        }
    }
}
