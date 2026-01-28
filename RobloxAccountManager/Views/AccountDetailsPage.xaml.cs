using System.Windows;
using System.Windows.Controls;
using RobloxAccountManager.ViewModels;

namespace RobloxAccountManager.Views
{
    public partial class AccountDetailsPage : Page
    {
        public AccountDetailsPage()
        {
            InitializeComponent();
            if (Application.Current.MainWindow.DataContext is MainViewModel vm)
                DataContext = vm.AccountDetailsVM;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AccountDetailsViewModel vm)
            {
                vm.NavigateBack();
            }
        }
    }
}
