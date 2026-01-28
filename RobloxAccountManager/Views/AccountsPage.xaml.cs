using System.Windows;
using System.Windows.Controls;
using RobloxAccountManager.ViewModels;
using RobloxAccountManager.Models;

namespace RobloxAccountManager.Views
{
    public partial class AccountsPage : Page
    {
        public AccountsPage()
        {
            InitializeComponent();
            if (Application.Current.MainWindow.DataContext is MainViewModel vm)
                DataContext = vm;
        }

        private void BtnAddAccount_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var mw = Application.Current.MainWindow as MainWindow;
                mw?.ShowLoginDialog();
            }
        }

        private void BtnEditAccount_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is RobloxAccount account)
            {
                 if (DataContext is MainViewModel vm)
                {
                    vm.NavigateAccountDetails(account);
                }
            }
        }

        private void BtnDeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is RobloxAccount account)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.RemoveAccountCommand.Execute(account);
                }
            }
        }
    }
}
