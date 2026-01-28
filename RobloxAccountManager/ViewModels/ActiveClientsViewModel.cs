using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobloxAccountManager.Models;
using RobloxAccountManager.Services;
using System.Collections.ObjectModel;

namespace RobloxAccountManager.ViewModels
{
    public partial class ActiveClientsViewModel : ObservableObject
    {
        private readonly RobloxProcessManager _processManager;

        public ObservableCollection<RobloxSession> Sessions => _processManager.ActiveSessions;

        public ActiveClientsViewModel(RobloxProcessManager processManager)
        {
            _processManager = processManager;
        }

        [RelayCommand]
        public void CloseSession(RobloxSession? session)
        {
             if (session != null)
             {
                 session.Kill();
                 // Handled via Exited event
             }
        }
    }
}
