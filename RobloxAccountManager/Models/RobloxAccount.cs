using CommunityToolkit.Mvvm.ComponentModel;

namespace RobloxAccountManager.Models
{
    public partial class RobloxAccount : ObservableObject
    {
        [ObservableProperty]
        private string _username = "New Account";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasAlias))]
        private string _alias = string.Empty; // User-defined local alias

        public bool HasAlias => !string.IsNullOrEmpty(Alias);

        [ObservableProperty]
        private string _description = string.Empty; // User notes/description

        [ObservableProperty]
        private string _group = "Default"; // Account Group (e.g. Main, Alt)

        [ObservableProperty]
        private string _proxyUrl = ""; // HTTP Proxy (e.g. http://user:pass@host:port)

        [ObservableProperty]
        private string _displayName = string.Empty; // Roblox Display Name

        [ObservableProperty]
        private long _userId;

        [ObservableProperty]
        private string _cookieCipher = string.Empty; // Encrypted using SecurityService

        [ObservableProperty]
        private DateTime? _expirationDate;

        [ObservableProperty]
        private string _avatarUrl = "https://tr.rbxcdn.com/53eb9b17fe1432a809c73a132d78f5f1/150/150/AvatarHeadshot/Png";

        [ObservableProperty]
        private bool _isSelected;

        public RobloxAccount()
        {
        }
    }
}
