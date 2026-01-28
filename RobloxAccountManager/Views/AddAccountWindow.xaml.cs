using System.Windows;

namespace RobloxAccountManager.Views
{
    public partial class AddAccountWindow : Window
    {
        public string Username { get; private set; } = string.Empty;
        public string Cookie { get; private set; } = string.Empty;

        public AddAccountWindow()
        {
            InitializeComponent();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            Username = TxtUsername.Text;
            Cookie = TxtCookie.Text;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Cookie))
            {
                MessageBox.Show("Please enter both a username and the security cookie.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
