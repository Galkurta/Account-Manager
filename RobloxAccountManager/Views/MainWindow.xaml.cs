using System.Windows;
using Wpf.Ui.Controls;
using RobloxAccountManager.ViewModels;

namespace RobloxAccountManager.Views
{
    public partial class MainWindow : FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            vm.LaunchRequested += OnLaunchRequested;
            vm.NavigationHandler = (pageType) => RootNavigation.Navigate(pageType);
            DataContext = vm;
            
            Loaded += (s, e) => RootNavigation.Navigate(typeof(AccountsPage));
        }

        private async void OnLaunchRequested(object? sender, System.EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var dialog = new LaunchOptionsContentDialog(RootContentDialog);
                await dialog.ShowAsync();

                if (dialog.Launched)
                {
                    await vm.LaunchSelectedAsync(dialog.PlaceId, dialog.JobId);
                }
            }
        }

        public ContentDialogHost GetDialogHost() => RootContentDialog;

        public async System.Threading.Tasks.Task ShowAlertAsync(string title, string message)
        {
            var dialog = new ContentDialog(RootContentDialog)
            {
                Title = title,
                Content = new TextBlock { Text = message, TextWrapping = System.Windows.TextWrapping.Wrap },
                CloseButtonText = "OK"
            };
            await dialog.ShowAsync();
        }

        public async void ShowLoginDialog()
        {
            var loginDialog = new LoginContentDialog(RootContentDialog);
            await loginDialog.ShowAsync();

            if (DataContext is MainViewModel vm)
            {
                string cookie = loginDialog.ScrapedCookie;
                if (!string.IsNullOrEmpty(cookie))
                {
                    await vm.AddNewAccountAsync(cookie, loginDialog.ScrapedCookieExpiration);
                }
            }
        }
    }
}
