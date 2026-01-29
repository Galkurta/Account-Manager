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
        private readonly WindowLayoutService _windowLayoutService;

        public ObservableCollection<RobloxSession> Sessions => _processManager.ActiveSessions;

        private readonly MainViewModel _mainViewModel;

        public ActiveClientsViewModel(MainViewModel main, RobloxProcessManager processManager)
        {
            _mainViewModel = main;
            _processManager = processManager;
            _windowLayoutService = new WindowLayoutService();
        }

        [RelayCommand]
        public void NavigateBack()
        {
            _mainViewModel.NavigateAccounts();
        }

        [RelayCommand]
        public void TileWindows()
        {
            LogService.Log("Tiled windows.", LogLevel.Info, "Layout");
            _windowLayoutService.TileWindows();
        }

        [RelayCommand]
        public void CascadeWindows()
        {
             LogService.Log("Cascaded windows.", LogLevel.Info, "Layout");
            _windowLayoutService.CascadeWindows();
        }

        [RelayCommand]
        public void MinimizeAll()
        {
             LogService.Log("Minimized all windows.", LogLevel.Info, "Layout");
            _windowLayoutService.MinimizeAll();
        }

        [RelayCommand]
        public void CloseSession(RobloxSession? session)
        {
             if (session != null)
             {
                 LogService.Log($"Requested close for session: {session.AccountName} (PID: {session.ProcessId})", LogLevel.Info, "Session");
                 session.Kill();
                 // Handled via Exited event
             }
        }
    }
}
