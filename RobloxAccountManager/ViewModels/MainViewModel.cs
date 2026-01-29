using System;
using System.Windows;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobloxAccountManager.Models;
using RobloxAccountManager.Services;
using System.Linq;
using System.Threading.Tasks;

namespace RobloxAccountManager.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private string _logOutput = string.Empty;

        private readonly RobloxProcessManager _processManager;
        private readonly SecurityService _securityService;
        private readonly AccountStorageService _storageService;
        private readonly RobloxRequestService _requestService;


        public ExploitStatusViewModel ExploitsVM { get; }
        public VersionManagerViewModel VersionsVM { get; }
        public SettingsViewModel SettingsVM { get; }
        public AccountDetailsViewModel AccountDetailsVM { get; }
        public AddFriendViewModel AddFriendVM { get; }
        public LogsViewModel LogsVM { get; }
        public AboutViewModel AboutVM { get; }
        public AutoJoinViewModel AutoJoinVM { get; }
        public ActiveClientsViewModel ActiveClientsVM { get; }
        public BrowserViewModel BrowserVM { get; }

        public Action<Type>? NavigationHandler { get; set; }

        public ObservableCollection<RobloxAccount> Accounts { get; } = new ObservableCollection<RobloxAccount>();

        public MainViewModel()
        {

            _processManager = new RobloxProcessManager();
            _securityService = new SecurityService();
            _storageService = new AccountStorageService();
            _requestService = new RobloxRequestService();
            

            ExploitsVM = new ExploitStatusViewModel();
            VersionsVM = new VersionManagerViewModel();
            SettingsVM = new SettingsViewModel();
            var autoJoinService = new AutoJoinService(_requestService);

            

            autoJoinService.RelaunchCallback = async (userId, jobId, placeId) =>
            {
                Log($"[AutoJoin] Re-launching account {userId} (Place: {placeId}, Job: {jobId})...");
                

                RobloxAccount? account = null;
                await Application.Current.Dispatcher.InvokeAsync(() => 
                {
                    account = Accounts.FirstOrDefault(a => a.UserId == userId);
                });

                if (account == null) 
                {
                    Log($"[AutoJoin] Failed to find account {userId} for relaunch.");
                    return;
                }

                await Application.Current.Dispatcher.InvokeAsync(async () => 
                {
                    string cookie = _securityService.Decrypt(account.CookieCipher);
                    if (string.IsNullOrEmpty(cookie)) return;
                    

                    string result = await _processManager.LaunchAccount(cookie, account.UserId, account.Username, placeId, jobId);
                    Log($"[AutoJoin] Relaunch Result: {result}");
                });
            };


            _processManager.SessionJobIdUpdated += (uid, pid, jid) =>
            {
                autoJoinService.UpdateSessionInfo(uid, pid, jid);
            };

            AccountDetailsVM = new AccountDetailsViewModel(this);
            AddFriendVM = new AddFriendViewModel(this, _requestService, _securityService);
            AutoJoinVM = new AutoJoinViewModel(Accounts, autoJoinService, _securityService);
            BrowserVM = new BrowserViewModel(Accounts, _securityService);
            ActiveClientsVM = new ActiveClientsViewModel(_processManager);
            LogsVM = new LogsViewModel();
            AboutVM = new AboutViewModel();
            
            _statusMessage = "Ready";
            

            var savedAccounts = _storageService.LoadAccounts();
            foreach (var acc in savedAccounts)
            {
                Accounts.Add(acc);
            }

            CheckForUpdates();
        }

        private async void CheckForUpdates()
        {
            var updateService = new UpdateService();
            var release = await updateService.CheckForUpdatesAsync();
            if (release != null)
            {
                IsUpdateAvailable = true;
                UpdateVersion = release.TagName;
                _pendingUpdate = release;
                Log($"[Update] New version found: {release.TagName}");
            }
        }

        [ObservableProperty]
        private bool _isUpdateAvailable;

        [ObservableProperty]
        private string _updateVersion = string.Empty;

        private GithubRelease? _pendingUpdate;

        [RelayCommand]
        public async Task PerformUpdate()
        {
            if (_pendingUpdate == null) return;
            
            var updateService = new UpdateService();
            Log("Downloading update...");
            await updateService.DownloadAndInstallAsync(_pendingUpdate);
        }

        [RelayCommand]
        public void NavigateAccounts() => NavigationHandler?.Invoke(typeof(Views.AccountsPage));

        [RelayCommand]
        public void NavigateBrowser() => NavigationHandler?.Invoke(typeof(Views.BrowserPage));

        public void NavigateTo(Type pageType) => NavigationHandler?.Invoke(pageType);

        public event EventHandler? LaunchRequested;

        [RelayCommand]
        public void LaunchSelected()
        {

             LaunchRequested?.Invoke(this, EventArgs.Empty);
        }

        public void NavigateAccountDetails(RobloxAccount account)
        {
            AccountDetailsVM.LoadAccount(account);
            NavigationHandler?.Invoke(typeof(Views.AccountDetailsPage));
        }

        public async Task AddNewAccountAsync(string rawCookie, DateTime? expiration = null)
        {
            string encryptedCookie = _securityService.Encrypt(rawCookie);
            var newAccount = new RobloxAccount 
            { 
                Username = "Loading...", 
                CookieCipher = encryptedCookie,
                ExpirationDate = expiration,
                IsSelected = true
            };
            
            Accounts.Add(newAccount);
            _storageService.SaveAccounts(Accounts); // Save immediately
            
            Log($"Added account with captured cookie. Fetching profile...");

            try
            {
                var userInfo = await _processManager.GetUserInfo(rawCookie);
                if (userInfo != null)
                {
                    newAccount.Username = userInfo.Name ?? "Unknown";
                    newAccount.DisplayName = userInfo.DisplayName ?? "Unknown";
                    newAccount.UserId = userInfo.Id;
                    newAccount.AvatarUrl = userInfo.AvatarUrl ?? "https://tr.rbxcdn.com/53eb9b17fe1432a809c73a132d78f5f1/150/150/AvatarHeadshot/Png";
                    Log($"Identified account: {newAccount.Username} (Display: {newAccount.DisplayName}, ID: {newAccount.UserId})");
                    _storageService.SaveAccounts(Accounts); // Save updated info
                }
                else
                {
                    newAccount.Username = "Unknown";
                    Log($"[Warning] Failed to fetch user info.");
                }
            }
            catch (Exception ex)
            {
                Log($"[Error] Profile fetch failed: {ex.Message}");
                newAccount.Username = "Error";
            }
        }

        [RelayCommand]
        public void RemoveAccount(RobloxAccount? account)
        {
            if (account != null && Accounts.Contains(account))
            {
                Accounts.Remove(account);
                _storageService.SaveAccounts(Accounts); // Save after removal
                Log($"Removed account: {account.Username}");
            }
        }

        public void SaveAccounts()
        {
            _storageService.SaveAccounts(Accounts);
            Log("Accounts saved.");
        }

        public async Task LaunchSelectedAsync(string? placeId, string? jobId, string? accessCode = null)
        {
            var selectedAccounts = Accounts.Where(a => a.IsSelected).ToList();
            if (!selectedAccounts.Any())
            {
                Log("No accounts selected.");
                return;
            }

            Log($"Launching {selectedAccounts.Count} account(s)...");

            foreach (var account in selectedAccounts)
            {
                Log($"Preparing {account.Username}...");
                

                string cookie = _securityService.Decrypt(account.CookieCipher);
                if (string.IsNullOrEmpty(cookie))
                {
                    Log($"[Error] Failed to decrypt cookie for {account.Username}");
                    continue;
                }


                string result = await _processManager.LaunchAccount(cookie, account.UserId, account.Username, placeId, jobId, accessCode);
                Log($"[{account.Username}] Result: {result}");

                await Task.Delay(1000); // Stagger launches slightly
            }

            Log("Launch sequence completed.");
        }

        [RelayCommand]
        public void NavigateAddFriend()
        {
            NavigationHandler?.Invoke(typeof(Views.AddFriendPage));
        }

        private async Task ShowAlertAsync(string title, string message)
        {
            if (Application.Current.MainWindow is Views.MainWindow mw)
            {
                await mw.ShowAlertAsync(title, message);
            }
        }

        private void Log(string message)
        {
            StatusMessage = message; 
            LogService.Log(message);
        }
    }
}
