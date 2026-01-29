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
            
            try
            {
                this.Icon = new System.Windows.Media.Imaging.BitmapImage(new System.Uri("pack://application:,,,/RobloxAccountManager;component/Resources/app.ico"));
                ApplicationTitleBar.Icon = new Wpf.Ui.Controls.ImageIcon { Source = this.Icon };
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load app icon: {ex.Message}");
            }

            var vm = new MainViewModel();
            vm.LaunchRequested += OnLaunchRequested;
            vm.NavigationHandler = (pageType) => RootNavigation.Navigate(pageType);
            DataContext = vm;
            
            Loaded += (s, e) => 
            {
                RootNavigation.Navigate(typeof(AccountsPage));
                AdjustNavigationPaneWidth();
            };
        }

        private void AdjustNavigationPaneWidth()
        {
            double maxTextWidth = 0;
            var fontSize = 14.0; // Assuming default font size
            var typeface = new System.Windows.Media.Typeface(new System.Windows.Media.FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            foreach (var item in RootNavigation.MenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Content is string text)
                {
                    var formattedText = new System.Windows.Media.FormattedText(
                        text,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        fontSize,
                        System.Windows.Media.Brushes.Black,
                        new System.Windows.Media.NumberSubstitution(),
                        1);

                    if (formattedText.Width > maxTextWidth)
                        maxTextWidth = formattedText.Width;
                }
            }
            
            // Base width (icon + margins) + Text Width + Padding
            // Icon ~40px, Margins ~20px
            RootNavigation.OpenPaneLength = maxTextWidth + 60; 
        }

        private async void OnLaunchRequested(object? sender, System.EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var dialog = new LaunchOptionsContentDialog(RootContentDialog);
                await dialog.ShowAsync();

                if (dialog.Launched)
                {
                    await vm.LaunchSelectedAsync(dialog.PlaceId, dialog.JobId, dialog.AccessCode);
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

        public async System.Threading.Tasks.Task<string?> ShowInputDialogAsync(string title, string message, string defaultValue = "")
        {
            var textBox = new Wpf.Ui.Controls.TextBox
            {
                Text = defaultValue,
                Margin = new System.Windows.Thickness(0, 10, 0, 0)
            };

            var dialog = new ContentDialog(RootContentDialog)
            {
                Title = title,
                Content = new System.Windows.Controls.StackPanel
                {
                    Children = 
                    { 
                        new System.Windows.Controls.TextBlock { Text = message, TextWrapping = System.Windows.TextWrapping.Wrap },
                        textBox
                    }
                },
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                return textBox.Text;
            }
            return null;
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
