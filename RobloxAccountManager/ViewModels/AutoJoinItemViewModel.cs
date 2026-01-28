using CommunityToolkit.Mvvm.ComponentModel;
using RobloxAccountManager.Models;
using System;

namespace RobloxAccountManager.ViewModels
{
    public partial class AutoJoinItemViewModel : ObservableObject
    {
        public RobloxAccount Account { get; }

        [ObservableProperty]
        private bool _isEnabled;

        [ObservableProperty]
        private string _status = "Idle";

        // Event to notify parent ViewModel when toggle changes
        public event EventHandler<bool>? IsEnabledChanged;

        public AutoJoinItemViewModel(RobloxAccount account)
        {
            Account = account;
        }

        partial void OnIsEnabledChanged(bool value)
        {
            IsEnabledChanged?.Invoke(this, value);
        }

        public void UpdateStatus(string status)
        {
            Status = status;
        }
    }
}
