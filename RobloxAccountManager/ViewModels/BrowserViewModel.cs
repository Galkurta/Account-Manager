using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobloxAccountManager.Models;
using RobloxAccountManager.Services;
using RobloxAccountManager.Views;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace RobloxAccountManager.ViewModels
{
    public partial class BrowserViewModel : ObservableObject
    {
        private readonly SecurityService _securityService;

        [ObservableProperty]
        private ObservableCollection<BrowserAccountItem> _accountItems = new();

        public BrowserViewModel(ObservableCollection<RobloxAccount> accounts, SecurityService securityService)
        {
            _securityService = securityService;


            foreach (var acc in accounts)
            {
                AccountItems.Add(new BrowserAccountItem(acc));
            }


            accounts.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (RobloxAccount acc in e.NewItems)
                        AccountItems.Add(new BrowserAccountItem(acc));
                }
                if (e.OldItems != null)
                {
                    foreach (RobloxAccount acc in e.OldItems)
                    {
                        var item = AccountItems.FirstOrDefault(i => i.Account.UserId == acc.UserId);
                        if (item != null) AccountItems.Remove(item);
                    }
                }
            };
        }

        [RelayCommand]
        public void OpenSelected()
        {
            var selected = AccountItems.Where(x => x.IsSelected).ToList();

            if (!selected.Any())
            {
                MessageBox.Show("Please select at least one account to open.", "Browser", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (var item in selected)
            {

                var win = new BrowserWindow(item.Account, _securityService);
                win.Show();
            }
        }
    }

    public partial class BrowserAccountItem : ObservableObject
    {
        public RobloxAccount Account { get; }

        [ObservableProperty]
        private bool _isSelected;

        public BrowserAccountItem(RobloxAccount account)
        {
            Account = account;
            IsSelected = false; 
        }
    }
}
