using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobloxAccountManager.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RobloxAccountManager.ViewModels
{
    public partial class AddFriendViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;
        private readonly RobloxRequestService _requestService;
        private readonly SecurityService _securityService;

        [ObservableProperty]
        private string _targetInput = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasTargetUser))]
        private RobloxUserInfo? _targetUser;
        
        public bool HasTargetUser => TargetUser != null;

        [ObservableProperty]
        private bool _isBusy;

        public AddFriendViewModel(MainViewModel main, RobloxRequestService requestService, SecurityService securityService)
        {
            _mainViewModel = main;
            _requestService = requestService;
            _securityService = securityService;
        }

        [RelayCommand]
        public void NavigateBack()
        {
            _mainViewModel.NavigateAccounts();
            TargetInput = string.Empty;
            TargetUser = null;
        }

        [RelayCommand]
        public async Task SearchUserAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            TargetUser = null;

            try
            {
                string input = TargetInput.Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Log("Please enter a username or ID.");
                    return;
                }

                Log($"Searching for '{input}'...");

                long targetId = 0;
                

                if (long.TryParse(input, out long parsedId))
                {
                    targetId = parsedId;
                }
                else
                {

                     var lookupId = await _requestService.GetUserIdFromUsernameAsync(input);
                    if (lookupId.HasValue)
                    {
                        targetId = lookupId.Value;
                    }
                    else
                    {
                        Log($"User '{input}' not found.");
                        return;
                    }
                }


                var info = await _requestService.GetUserInfoAsync(targetId);
                if (info != null)
                {
                    TargetUser = info;
                    Log($"Found: {info.DisplayName} (@{info.Name})");
                }
                else
                {
                    Log($"Could not fetch details for ID {targetId}.");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task SendRequestsAsync()
        {
            if (IsBusy) return;
            
            if (TargetUser == null)
            {
                // If they click send but haven't searched, try searching first
                await SearchUserAsync();
                
                if (TargetUser == null) return;
            }

            IsBusy = true;
            
            try
            {
                var selectedAccounts = _mainViewModel.Accounts.Where(a => a.IsSelected).ToList();
                if (!selectedAccounts.Any())
                {
                    Log("No accounts selected! Select accounts in the Home tab.");
                    return;
                }

                Log($"Sending friend requests to @{TargetUser.Name} ({TargetUser.Id})...");
                Log($"Using {selectedAccounts.Count} account(s).");

                foreach (var account in selectedAccounts)
                {
                    string cookie = _securityService.Decrypt(account.CookieCipher);
                    if (string.IsNullOrEmpty(cookie))
                    {
                        Log($"[Skip] {account.Username}: Cookie Error");
                        continue;
                    }

                    var (success, msg) = await _requestService.SendFriendRequestAsync(cookie, TargetUser.Id);
                    string status = success ? "Success" : "Failed";
                    Log($"[{status}] {account.Username}: {msg}");

                    await Task.Delay(500); // Rate limit
                }

                Log("Batch completed.");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private void Log(string msg)
        {
            LogService.Log(msg);
        }
    }
}
