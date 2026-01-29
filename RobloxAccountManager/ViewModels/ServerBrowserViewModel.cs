using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobloxAccountManager.Services;
using System.Linq;
using System.Windows;

namespace RobloxAccountManager.ViewModels
{
    public partial class ServerBrowserViewModel : ObservableObject
    {
        private readonly RobloxGameService _gameService;

        [ObservableProperty]
        private string _placeId = string.Empty;

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private RobloxServer? _selectedServer;


        [ObservableProperty]
        private string _gameTitle = "Unknown Game";

        [ObservableProperty]
        private string _gameCreator = "";

        [ObservableProperty]
        private string _gameDescription = "";

        [ObservableProperty]
        private string _thumbnailUrl = "";

        [ObservableProperty]
        private string _playingCount = "-";

        [ObservableProperty]
        private string _visitsCount = "-";

        [ObservableProperty]
        private string _likesCount = "-";

        [ObservableProperty] 
        private bool _hasGameDetails;

        [ObservableProperty]
        private bool _isPrivateServerMode;

        public ObservableCollection<RobloxServer> Servers { get; } = new ObservableCollection<RobloxServer>();

        public ServerBrowserViewModel()
        {
            _gameService = new RobloxGameService();
            _statusMessage = "Enter a Place ID to fetch servers.";
            _hasGameDetails = false;
        }

        [RelayCommand]
        private async Task FetchServers()
        {
            if (string.IsNullOrWhiteSpace(PlaceId) || !long.TryParse(PlaceId, out long pid))
            {
                StatusMessage = "Invalid Place ID.";
                LogService.Error($"Invalid Place ID entered: {PlaceId}", "Browser");
                HasGameDetails = false;
                return;
            }

            IsLoading = true;
            StatusMessage = "Fetching game details...";
            LogService.Log($"Fetching details for Place ID {pid}...", LogLevel.Info, "Browser");
            Servers.Clear();
            HasGameDetails = false;


            var universeId = await _gameService.GetUniverseId(pid);
            if (universeId.HasValue)
            {

                var details = await _gameService.GetGameDetails(universeId.Value);
                if (details != null)
                {
                    GameTitle = details.Name ?? "Unknown";
                    GameCreator = $"by {details.CreatorName ?? "Unknown"}";
                    GameDescription = details.Description ?? "No description.";
                    PlayingCount = $"{details.Playing:N0} Playing";
                    VisitsCount = $"{details.Visits:N0} Visits";
                    LikesCount = $"{details.FavoritedCount:N0} Likes"; 
                    HasGameDetails = true;
                    LogService.Log($"Found Game '{GameTitle}' (Univ: {universeId})", LogLevel.Info, "Browser");
                }


                ThumbnailUrl = await _gameService.GetGameIcon(universeId.Value) ?? string.Empty;
            }
            else
            {
                 LogService.Error($"Could not resolve Universe ID for Place {pid}", "Browser");
            }

            StatusMessage = "Fetching servers...";
            

            if (IsPrivateServerMode)
            {
                string? cookie = null;
                // Try to get cookie from currently selected account in MainViewModel
                if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVM)
                {
                    var selectedAccount = mainVM.Accounts.FirstOrDefault(a => a.IsSelected);
                    if (selectedAccount != null)
                    {
                         var secService = new SecurityService(); 
                         cookie = secService.Decrypt(selectedAccount.CookieCipher);
                    }
                }

                if (string.IsNullOrEmpty(cookie))
                {
                    StatusMessage = "Authentication required (Select an account in main window).";
                    IsLoading = false;
                    return;
                }

                var privateServers = await _gameService.GetPrivateServers(pid, cookie);
                foreach (var s in privateServers)
                {
                    Servers.Add(s);
                }
            }
            else
            {
                var servers = await _gameService.GetPublicServers(pid);
                foreach (var s in servers)
                {
                    Servers.Add(s);
                }
            }

            string resultMsg = Servers.Count == 0 ? "No servers found." : $"Found {Servers.Count} servers.";
            StatusMessage = resultMsg;
            LogService.Log($"{resultMsg}", LogLevel.Info, "Browser");
            
            IsLoading = false;
        }
    }
}
