using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobloxAccountManager.Models;
using System.Windows.Media.Imaging;

namespace RobloxAccountManager.ViewModels
{
    public partial class AccountDetailsViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        [ObservableProperty]
        private RobloxAccount? _selectedAccount;

        [ObservableProperty]
        private string _aliasEditBuffer = string.Empty;

        [ObservableProperty]
        private string _descriptionEditBuffer = string.Empty;

        [ObservableProperty]
        private string _username = "";

        [ObservableProperty]
        private string _userId = "";

        [ObservableProperty]
        private string _avatarUrl = "";

        [ObservableProperty]
        private string _expirationText = "Unknown";

        public AccountDetailsViewModel(MainViewModel main)
        {
            _mainViewModel = main;
        }

        public void LoadAccount(RobloxAccount account)
        {
            SelectedAccount = account;
            Username = account.Username;
            UserId = account.UserId.ToString();
            AvatarUrl = account.AvatarUrl;
            AliasEditBuffer = account.Alias;
            DescriptionEditBuffer = account.Description;
            
            ExpirationText = account.ExpirationDate.HasValue 
                ? account.ExpirationDate.Value.ToString("g") 
                : "No expiration info";
        }

        [RelayCommand]
        public void SaveAndBack()
        {
            if (SelectedAccount != null)
            {
                SelectedAccount.Alias = AliasEditBuffer;
                SelectedAccount.Description = DescriptionEditBuffer;
                _mainViewModel.SaveAccounts();
            }
            _mainViewModel.NavigateAccounts();
        }

        [RelayCommand]
        public void NavigateBack()
        {
            _mainViewModel.NavigateAccounts();
        }
    }
}
