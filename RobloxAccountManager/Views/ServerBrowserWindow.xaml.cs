using System.Windows;
using Wpf.Ui.Controls;
using RobloxAccountManager.Services;
using RobloxAccountManager.ViewModels;

namespace RobloxAccountManager.Views
{
    public partial class ServerBrowserWindow : FluentWindow
    {
        public string SelectedJobId { get; private set; } = string.Empty;
        public string SelectedPlaceId { get; private set; } = string.Empty;

        public ServerBrowserWindow(string initialPlaceId = "")
        {
            InitializeComponent();
            var vm = new ServerBrowserViewModel();
            vm.PlaceId = initialPlaceId;
            DataContext = vm;
        }

        private void BtnSetTarget_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ServerBrowserViewModel vm && vm.SelectedServer != null)
            {
                SelectedJobId = vm.SelectedServer.Id;
                SelectedPlaceId = vm.PlaceId;
                DialogResult = true;
                Close();
            }
            else
            {
                System.Windows.MessageBox.Show("Please select a server from the list first.", "No Selection", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
    }
}
