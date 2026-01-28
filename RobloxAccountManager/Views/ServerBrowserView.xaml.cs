using System;
using System.Windows;
using System.Windows.Controls;
using RobloxAccountManager.ViewModels;

namespace RobloxAccountManager.Views
{
    public partial class ServerBrowserView : UserControl
    {
        public event Action<string, string>? ServerSelected;
        public event Action? BrowserCanceled;

        public ServerBrowserView()
        {
            InitializeComponent();
            // Default Constructor needed for XAML, we set DataContext later or via Property
            // But logic expected initialPlaceId. We can expose a method Initialize(string id).
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
                      ServerSelected?.Invoke(vm.PlaceId, vm.SelectedServer.Id);
                 }
                 else
                 {
                     // Show Alert
                     if (Application.Current.MainWindow is MainWindow mw)
                     {
                         // Dialog over dialog/flyout might be tricky but ShowAlertAsync creates a ContentDialog 
                         // on the Main Window's presenter. If Flyout is open, Dialog appears under? or over?
                         // Flyout is a lightweight Popup. Dialog is in Visual Tree.
                         // Dialog usually appears in the ContentPresenter. If ContentPresenter is under Flyout's Popup layer,
                         // Flyout stays on top.
                         // However, Flyout is "Light Dismiss". Clicking the Dialog (if strictly modal) might be blocked by Flyout?
                         // Actually, Flyout closes if you click outside.
                         // If I show a dialog, the user might see it behind the flyout.
                         // Better to show an inline error or just close flyout?
                         // I'll try showing the alert.
                         await mw.ShowAlertAsync("No Selection", "Please select a server from the list first.");
                     }
                 }
            }
        }
    }
}
