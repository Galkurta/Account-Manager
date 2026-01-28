using System.Windows;
using System.Windows.Controls;
using RobloxAccountManager.ViewModels;

namespace RobloxAccountManager.Views
{
    public partial class BrowserPage : Page
    {
        public BrowserPage()
        {
            InitializeComponent();
            if (Application.Current.MainWindow.DataContext is MainViewModel vm)
                DataContext = vm.BrowserVM;
        }
    }
}
