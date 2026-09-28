using System.Windows;
using System.Windows.Controls;
using WorkshopInventory.Data;
using WorkshopInventory.Models;
using WorkshopInventory.Services;

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
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Введите название материала");
                return;
            }

            if (!decimal.TryParse(QtyBox.Text, out var qty)) qty = 0;
            if (!decimal.TryParse(MinQtyBox.Text, out var minQty)) minQty = 0;
            var location = string.IsNullOrWhiteSpace(LocationBox.Text) ? StockService.DefaultLocation : LocationBox.Text.Trim();

            var material = new Material
            {
                Name = NameBox.Text.Trim(),
                Unit = string.IsNullOrWhiteSpace(UnitBox.Text) ? "шт" : UnitBox.Text.Trim(),
                Category = CategoryBox.Text.Trim(),
                MinQuantity = minQty
            };
            _db.Materials.Add(material);
            _db.SaveChanges();

            if (qty > 0)
                _stock.RegisterReceipt(material.Id, qty, "Начальный остаток", _user.Id, location);

            NameBox.Clear();
            CategoryBox.Clear();
            QtyBox.Text = "0";
            MinQtyBox.Text = "0";
            LocationBox.Text = StockService.DefaultLocation;
            Refresh();
        }
    }
}
