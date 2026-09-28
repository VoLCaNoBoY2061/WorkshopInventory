using System.Windows;
using System.Windows.Controls;
using WorkshopInventory.Data;
using WorkshopInventory.Models;
using WorkshopInventory.Services;

namespace WorkshopInventory.Views
{
    public partial class MainWindow : Window
    {
        private readonly AppDbContext _db;
        private readonly StockService _stock;
        private readonly RequestService _requests;
        private readonly User _user;

        public MainWindow(User user)
        {
            InitializeComponent();
            _user = user;
            _db = new AppDbContext();
            _stock = new StockService(_db);
            _requests = new RequestService(_db, _stock);

            HeaderText.Text = $"{_user.FullName}   •   роль: {_user.Role}";

            MaterialsTab.Initialize(_db, _stock, _user);
            StorekeeperOpsTab.Initialize(_db, _stock, _user);
            RequestsTab.Initialize(_db, _requests, _user);
            ReportsTab.Initialize(_db, _stock, _user);

            // Вкладки видны только тем ролям, которым они нужны по use case диаграмме
            StorekeeperTab.Visibility = _user.Role == UserRole.Кладовщик ? Visibility.Visible : Visibility.Collapsed;
            ReportsTabItem.Visibility = _user.Role == UserRole.Мастер ? Visibility.Collapsed : Visibility.Visible;

            // Данные меняются из разных вкладок (а на практике — и с разных запущенных
            // экземпляров программы), поэтому при каждом переключении вкладки обновляем
            // её содержимое из базы. Это заменяет отсутствие "живых" уведомлений между
            // вкладками в упрощённой (код-behind, не MVVM) архитектуре.
            MainTabs.SelectionChanged += OnTabChanged;
        }

        private void OnTabChanged(object sender, SelectionChangedEventArgs e)
        {
            // Внутри ReportsView есть свой вложенный TabControl — его переключение тоже
            // всплывает как SelectionChanged, но это не смена главной вкладки, игнорируем.
            if (e.Source != MainTabs) return;

            MaterialsTab.Refresh();
            if (StorekeeperTab.Visibility == Visibility.Visible) StorekeeperOpsTab.Refresh();
            RequestsTab.Refresh();
            if (ReportsTabItem.Visibility == Visibility.Visible) ReportsTab.Refresh();
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Освобождаем ресурсы базы данных текущего пользователя
            _db.Dispose();

            // 2. Создаем и показываем окно входа заново
            var loginWindow = new LoginWindow();
            loginWindow.Show();

            // 3. Закрываем текущее главное окно
            this.Close();
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _db.Dispose();
        }

        private void MaterialsTab_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
