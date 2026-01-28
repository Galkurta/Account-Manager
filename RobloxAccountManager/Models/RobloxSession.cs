using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RobloxAccountManager.Models
{
    public partial class RobloxSession : ObservableObject
    {
        [ObservableProperty]
        private int _processId;

        [ObservableProperty]
        private string _accountName = string.Empty;

        [ObservableProperty]
        private long _userId;

        [ObservableProperty]
        private DateTime _launchTime;

        [ObservableProperty]
        private long _browserTrackerId;

        [ObservableProperty]
        private string _status = "Initializing";

        [ObservableProperty]
        private string _launchMode = "Direct";

        [ObservableProperty]
        private string? _placeId;

        [ObservableProperty]
        private string? _jobId;

        [ObservableProperty]
        private string? _placeName;

        // Helper to kill the process
        public void Kill()
        {
            try
            {
                var proc = System.Diagnostics.Process.GetProcessById(ProcessId);
                proc.Kill();
            }
            catch { }
        }
    }
}
