using System.Windows.Controls;
using RobloxAccountManager.ViewModels;

namespace RobloxAccountManager.Views
{
    public partial class ActiveClientsPage : Page
    {
        public ActiveClientsPage(ActiveClientsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        // Default constructor for design/nav usage if needed, though we prefer DI
        public ActiveClientsPage()
        {
            InitializeComponent();
            // If we have a global locator or static ref, use it. 
            // Otherwise MainViewModel/Navigation logic should provide the VM.
            // For now, if navigated via Type, we rely on the NavigationService.
            // But we need to ensure the DataContext is set.
            // The MainViewModel logic (Navigate method) generally instantiates pages or we use a frame.
            // Let's assume the Navigation system handles it.
            // Wait, standard Frame navigation creates a new instance.
            // We need a way to pass the singleton VM.
            // I'll grab it from App.Current if possible or MainViewModel.
            if (System.Windows.Application.Current.MainWindow.DataContext is MainViewModel mainVM)
            {
                DataContext = mainVM.ActiveClientsVM;
            }
        }
    }
}
