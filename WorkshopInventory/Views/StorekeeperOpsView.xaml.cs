using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WorkshopInventory.Data;
using WorkshopInventory.Models;
using WorkshopInventory.Services;

namespace WorkshopInventory.Views
{
    /// <summary>Строка объединённой истории операций (приход/списание/перемещение) для истории и отчётов.</summary>
    public class OperationRow
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    public partial class StorekeeperOpsView : UserControl
    {
        private AppDbContext _db = null!;
        private StockService _stock = null!;
        private User _user = null!;

        public StorekeeperOpsView()
        {
            InitializeComponent();
        }

        public void Initialize(AppDbContext db, StockService stock, User user)
        {
            _db = db;
            _stock = stock;
            _user = user;
            Refresh();
        }

        public void Refresh()
        {
            LoadMaterials();
            LoadLocations();
            RefreshHistory();
        }

        private void LoadMaterials()
        {
            var materials = _db.Materials.OrderBy(m => m.Name).ToList();
            ReceiptMaterial.ItemsSource = materials;
            WriteOffMaterial.ItemsSource = materials.ToList();
            MovementMaterial.ItemsSource = materials.ToList();
        }

        private void LoadLocations()
        {
            var locations = _stock.GetLocations();

            ReceiptLocation.ItemsSource = locations;
            if (string.IsNullOrEmpty(ReceiptLocation.Text)) ReceiptLocation.Text = StockService.DefaultLocation;

            WriteOffLocation.ItemsSource = locations;
            if (string.IsNullOrEmpty(WriteOffLocation.Text)) WriteOffLocation.Text = StockService.DefaultLocation;

            var previousFrom = MovementFrom.SelectedItem as string;
            MovementFrom.ItemsSource = locations;
            MovementFrom.SelectedItem = previousFrom != null && locations.Contains(previousFrom)
                ? previousFrom
                : locations.FirstOrDefault();

            MovementTo.ItemsSource = locations;
        }

        private void RefreshHistory()
        {
            var rows = new List<OperationRow>();
            rows.AddRange(_db.Receipts.Select(r => new OperationRow
            {
                Date = r.Date, Type = "Поступление", MaterialName = r.Material!.Name,
                Quantity = r.Quantity, Details = $"{r.Supplier} → {r.Location}"
            }));
            rows.AddRange(_db.WriteOffs.Select(w => new OperationRow
            {
                Date = w.Date, Type = "Списание", MaterialName = w.Material!.Name,
                Quantity = w.Quantity, Details = $"{w.Reason} ({w.Location})"
            }));
            rows.AddRange(_db.Movements.Select(m => new OperationRow
            {
                Date = m.Date, Type = "Перемещение", MaterialName = m.Material!.Name,
                Quantity = m.Quantity, Details = $"{m.FromLocation} → {m.ToLocation}"
            }));

            HistoryGrid.ItemsSource = rows.OrderByDescending(r => r.Date).Take(100).ToList();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => Refresh();

        private void OnReceipt(object sender, RoutedEventArgs e)
        {
            if (ReceiptMaterial.SelectedItem is not Material material) { MessageBox.Show("Выберите материал"); return; }
            if (!decimal.TryParse(ReceiptQty.Text, out var qty) || qty <= 0) { MessageBox.Show("Некорректное количество"); return; }
            var location = string.IsNullOrWhiteSpace(ReceiptLocation.Text) ? StockService.DefaultLocation : ReceiptLocation.Text.Trim();

            try
            {
                _stock.RegisterReceipt(material.Id, qty, ReceiptSupplier.Text.Trim(), _user.Id, location);
                ReceiptQty.Clear();
                ReceiptSupplier.Clear();
                Refresh();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void OnWriteOff(object sender, RoutedEventArgs e)
        {
            if (WriteOffMaterial.SelectedItem is not Material material) { MessageBox.Show("Выберите материал"); return; }
            if (!decimal.TryParse(WriteOffQty.Text, out var qty) || qty <= 0) { MessageBox.Show("Некорректное количество"); return; }
            var location = string.IsNullOrWhiteSpace(WriteOffLocation.Text) ? StockService.DefaultLocation : WriteOffLocation.Text.Trim();

            try
            {
                _stock.RegisterWriteOff(material.Id, qty, WriteOffReason.Text.Trim(), _user.Id, location);
                WriteOffQty.Clear();
                WriteOffReason.Clear();
                Refresh();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void OnMovement(object sender, RoutedEventArgs e)
        {
            if (MovementMaterial.SelectedItem is not Material material) { MessageBox.Show("Выберите материал"); return; }
            if (!decimal.TryParse(MovementQty.Text, out var qty) || qty <= 0) { MessageBox.Show("Некорректное количество"); return; }
            if (MovementFrom.SelectedItem is not string from || string.IsNullOrWhiteSpace(from))
            {
                MessageBox.Show("Выберите склад отправления");
                return;
            }

            var to = string.IsNullOrWhiteSpace(MovementTo.Text) ? MovementTo.SelectedItem as string : MovementTo.Text.Trim();
            if (string.IsNullOrWhiteSpace(to)) { MessageBox.Show("Укажите склад назначения"); return; }

            try
            {
                _stock.RegisterMovement(material.Id, qty, from, to!, _user.Id);
                MovementQty.Clear();
                MovementTo.Text = string.Empty;
                Refresh();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры, точку и запятую
            Regex regex = new Regex("[^0-9.,]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
