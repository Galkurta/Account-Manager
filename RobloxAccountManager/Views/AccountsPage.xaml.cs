using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls; // For SymbolIcon
using RobloxAccountManager.ViewModels;
using RobloxAccountManager.Models;
using System.Linq;
using System.Collections.Generic;

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
        private void MnuMoveToGroup_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem && DataContext is MainViewModel vm)
            {
                menuItem.Items.Clear();

                // "New Group..." Option
                var newGroupItem = new System.Windows.Controls.MenuItem { Header = "New Group...", Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Add24 } };
                newGroupItem.Click += BtnCreateNewGroup_Click;
                newGroupItem.Tag = menuItem.DataContext; // Pass the RobloxAccount
                menuItem.Items.Add(newGroupItem);

                menuItem.Items.Add(new System.Windows.Controls.Separator());

                // Existing Groups
                var groups = vm.Accounts.Select(a => a.Group).Distinct().OrderBy(g => g).ToList();
                
                // Always ensure "Default" is there or handled by list
                if (!groups.Contains("Default")) groups.Insert(0, "Default");

                foreach (var group in groups)
                {
                    var groupItem = new System.Windows.Controls.MenuItem { Header = group };
                    groupItem.Click += BtnMoveToExistingGroup_Click;
                    groupItem.Tag = menuItem.DataContext; // Pass the RobloxAccount
                    menuItem.Items.Add(groupItem);
                }
            }
        }

        private async void BtnCreateNewGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem item && item.Tag is RobloxAccount account && DataContext is MainViewModel vm)
            {
                var mw = Application.Current.MainWindow as MainWindow;
                if (mw != null)
                {
                    string? newGroup = await mw.ShowInputDialogAsync("New Group", "Enter a name for the new group:");
                    if (!string.IsNullOrWhiteSpace(newGroup))
                    {
                        await vm.MoveAccountToGroupAsync(account, newGroup.Trim());
                    }
                }
            }
        }

        private async void BtnMoveToExistingGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem item && item.Tag is RobloxAccount account && DataContext is MainViewModel vm)
            {
                if (item.Header is string groupName)
                {
                    await vm.MoveAccountToGroupAsync(account, groupName);
                }
            }
        }

        private async void BtnDeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string groupName && DataContext is MainViewModel vm)
            {
                if (groupName == "Default") return; // Safety check

                // Direct delete for now, assuming user intent.
                await vm.DeleteGroupAsync(groupName);
            }
        }
    }
}
