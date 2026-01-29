using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobloxAccountManager.Services;
using System.Windows;

namespace RobloxAccountManager.ViewModels
{
    public partial class LogsViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        public LogsViewModel(MainViewModel main)
        {
            _mainViewModel = main;
        }

        [RelayCommand]
        public void NavigateBack()
        {
            _mainViewModel?.NavigateAccounts();
        }
    }
}
