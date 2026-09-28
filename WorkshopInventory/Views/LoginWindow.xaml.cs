using System.Windows;
using WorkshopInventory.Data;
using WorkshopInventory.Services;

namespace WorkshopInventory.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void OnLoginClick(object sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            DbInitializer.Initialize(db);

            var user = AuthService.Login(db, LoginBox.Text.Trim(), PasswordBox.Password);
            if (user == null)
            {
                ErrorText.Text = "Неверный логин или пароль";
                return;
            }

            var main = new MainWindow(user);
            main.Show();
            Close();
        }
    }
}
