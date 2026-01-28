using System;
using System.Windows;
using System.Windows.Controls;
using RobloxAccountManager.ViewModels;

namespace RobloxAccountManager.Views
{
    public partial class ServerBrowserView : UserControl
    {
        public event Action<string, string, string?>? ServerSelected;
        public event Action? BrowserCanceled;

        public ServerBrowserView()
        {
            InitializeComponent();

        }
        
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            BrowserCanceled?.Invoke();
        }

        public void Initialize(string initialPlaceId)
        {
             var vm = new ServerBrowserViewModel();
             vm.PlaceId = initialPlaceId;
             DataContext = vm;
        }

        private async void BtnSetTarget_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ServerBrowserViewModel vm)
            {
                 if (vm.SelectedServer != null)
                 {
                      string? accessCode = (vm.SelectedServer is Services.RobloxPrivateServer ps) ? ps.AccessCode : null;
                      ServerSelected?.Invoke(vm.PlaceId, vm.SelectedServer.Id, accessCode);
                 }
                 else
                 {
                     // Show Alert
                     if (Application.Current.MainWindow is MainWindow mw)
                     {
                         await mw.ShowAlertAsync("No Selection", "Please select a server from the list first.");
                     }
                 }
            }
        }
    }
}
