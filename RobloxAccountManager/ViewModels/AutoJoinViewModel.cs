using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobloxAccountManager.Models;
using RobloxAccountManager.Services;
using System.Collections.ObjectModel;

using System.Linq;
using System.Windows;

namespace RobloxAccountManager.ViewModels
{
    public partial class AutoJoinViewModel : ObservableObject
    {
        private readonly AutoJoinService _autoJoinService;
        private readonly SecurityService _securityService;

        [ObservableProperty]
        private ObservableCollection<AutoJoinItemViewModel> _accountItems = new();
        
        // Pass-through setting
        public int AutoRejoinDelaySeconds
        {
            get => _autoJoinService.AutoRejoinDelaySeconds;
            set => _autoJoinService.AutoRejoinDelaySeconds = value;
        }

        public AutoJoinViewModel(ObservableCollection<RobloxAccount> accounts, AutoJoinService autoJoinService, SecurityService securityService)
        {
            _autoJoinService = autoJoinService;
            _securityService = securityService;
            
            // Transform accounts to items
            foreach (var acc in accounts)
            {
                var item = new AutoJoinItemViewModel(acc);
                item.IsEnabledChanged += Item_IsEnabledChanged;
                item.IsEnabled = _autoJoinService.IsMonitoring(acc.UserId);
                AccountItems.Add(item);
            }

            // Sync collection changes if accounts are added/removed dynamically (optional, but good practice)
            accounts.CollectionChanged += (s, e) => 
            {
               if (e.NewItems != null)
               {
                   foreach (RobloxAccount acc in e.NewItems)
                   {
                       var item = new AutoJoinItemViewModel(acc);
                       item.IsEnabledChanged += Item_IsEnabledChanged;
                       AccountItems.Add(item);
                   }
               }
               if (e.OldItems != null)
               {
                   foreach (RobloxAccount acc in e.OldItems)
                   {
                       var item = AccountItems.FirstOrDefault(i => i.Account.UserId == acc.UserId);
                       if (item != null)
                       {
                           item.IsEnabledChanged -= Item_IsEnabledChanged;
                           AccountItems.Remove(item);
                       }
                   }
               }
            };

            // Listen for service updates
            _autoJoinService.OnSessionStatusChanged += (userId, status) =>
            {
                var item = AccountItems.FirstOrDefault(i => i.Account.UserId == userId);
                if (item != null && item.IsEnabled)
                {
                    Application.Current.Dispatcher.Invoke(() => item.UpdateStatus(status));
                }
            };
        }

        private void Item_IsEnabledChanged(object? sender, bool isEnabled)
        {
            if (sender is AutoJoinItemViewModel item)
            {
                if (isEnabled)
                {
                    // Enable
                    try 
                    {
                        string cookie = _securityService.Decrypt(item.Account.CookieCipher);
                        if (!string.IsNullOrEmpty(cookie))
                        {
                             _autoJoinService.StartMonitoring(item.Account.UserId, cookie);
                             item.UpdateStatus("Monitoring");
                        }
                        else
                        {
                            item.IsEnabled = false; // Revert
                            item.UpdateStatus("Error: Cookie");
                        }
                    }
                    catch
                    {
                        item.IsEnabled = false;
                        item.UpdateStatus("Error: Decrypt");
                    }
                }
                else
                {
                    // Disable
                    _autoJoinService.StopMonitoring(item.Account.UserId);
                    item.UpdateStatus("Disabled");
                }
            }
        }
    }
}
