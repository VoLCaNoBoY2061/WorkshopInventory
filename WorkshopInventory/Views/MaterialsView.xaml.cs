using System.Windows;
using System.Windows.Controls;
using WorkshopInventory.Data;
using WorkshopInventory.Models;
using WorkshopInventory.Services;
using System.Text.RegularExpressions;
using System.Windows.Input;



namespace WorkshopInventory.Views
{
    /// <summary>Строка таблицы материалов: материал + остаток в текущем выбранном разрезе
    /// (конкретный склад или сумма по всем складам). Используется также в ReportsView.</summary>
    public class MaterialRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal MinQuantity { get; set; }
        public string Location { get; set; } = string.Empty;
        public bool IsBelowMinimum => Quantity < MinQuantity;
    }

    public partial class MaterialsView : UserControl
    {
        public const string AllLocations = "Все склады";

        private AppDbContext _db = null!;
        private StockService _stock = null!;
        private User _user = null!;
        private bool _suppressLocationEvent;

        public MaterialsView()
        {
            InitializeComponent();
        }

        public void Initialize(AppDbContext db, StockService stock, User user)
        {
            _db = db;
            _stock = stock;
            _user = user;
            AddPanel.Visibility = user.Role == UserRole.Кладовщик ? Visibility.Visible : Visibility.Collapsed;
            Refresh();
        }

        /// <summary>Полное обновление: перечитывает список складов (мог появиться новый после
        /// перемещения) и содержимое таблицы. Вызывается извне при переключении вкладок.</summary>
        public void Refresh()
        {
            if (_db == null) return;

            _suppressLocationEvent = true;
            var previouslySelected = LocationCombo.SelectedItem as string;

            var locations = new List<string> { AllLocations };
            locations.AddRange(_stock.GetLocations());
            LocationCombo.ItemsSource = locations;
            LocationCombo.SelectedItem = previouslySelected != null && locations.Contains(previouslySelected)
                ? previouslySelected
                : AllLocations;
            _suppressLocationEvent = false;

            RefreshGrid();
        }

        private void OnLocationChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressLocationEvent) return;
            RefreshGrid();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => Refresh();

        private void RefreshGrid()
        {
            var selectedLocation = LocationCombo.SelectedItem as string ?? AllLocations;

            Grid.ItemsSource = _db.Materials.OrderBy(m => m.Name).ToList().Select(m => new MaterialRow
            {
                Id = m.Id,
                Name = m.Name,
                Category = m.Category,
                Unit = m.Unit,
                MinQuantity = m.MinQuantity,
                Quantity = selectedLocation == AllLocations
                    ? _stock.GetTotalQuantity(m.Id)
                    : _stock.GetQuantityAt(m.Id, selectedLocation),
                Location = selectedLocation
            }).ToList();
        }

        private void OnAddMaterial(object sender, RoutedEventArgs e)
        {
            // 1. Проверка названия (с возвратом фокуса)
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите название материала.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameBox.Focus();
                return;
            }

            // 2. Проверка числовых полей (добавлена защита от отрицательных значений)
            if (!decimal.TryParse(QtyBox.Text, out decimal qty) || qty < 0)
            {
                MessageBox.Show("Начальный остаток должен быть числом больше или равным 0.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                QtyBox.Focus();
                return;
            }

            if (!decimal.TryParse(MinQtyBox.Text, out decimal minQty) || minQty < 0)
            {
                MessageBox.Show("Минимальный остаток должен быть числом больше или равным 0.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                MinQtyBox.Focus();
                return;
            }

            // 3. Определение склада (твоя хорошая логика с дефолтным значением сохранена)
            var location = string.IsNullOrWhiteSpace(LocationBox.Text) ? StockService.DefaultLocation : LocationBox.Text.Trim();

            try
            {
                // 4. Создание и сохранение материала
                var material = new Material
                {
                    Name = NameBox.Text.Trim(),
                    Unit = string.IsNullOrWhiteSpace(UnitBox.Text) ? "шт" : UnitBox.Text.Trim(),
                    Category = CategoryBox.Text.Trim(),
                    MinQuantity = minQty
                };

                _db.Materials.Add(material);
                _db.SaveChanges(); // Сохраняем, чтобы получить material.Id

                // 5. Регистрация начального остатка (если он > 0)
                if (qty > 0)
                {
                    _stock.RegisterReceipt(material.Id, qty, "Начальный остаток", _user, location);
                }

                // 6. Успешное завершение и очистка полей
                MessageBox.Show("Материал успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                NameBox.Clear();
                CategoryBox.Clear();
                UnitBox.Text = "шт"; // Возвращаем значение по умолчанию
                QtyBox.Text = "0";
                MinQtyBox.Text = "0";
                LocationBox.Text = StockService.DefaultLocation;

                Refresh(); // Обновляем таблицу
            }
            catch (Exception ex)
            {
                // 7. Обработка непредвиденных ошибок базы данных (чтобы приложение не "вылетало")
                MessageBox.Show($"Ошибка при сохранении в базу данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры, точку и запятую
            Regex regex = new Regex("[^0-9.,]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
