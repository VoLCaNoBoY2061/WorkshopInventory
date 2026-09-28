using System.Windows.Controls;
using WorkshopInventory.Data;
using WorkshopInventory.Models;
using WorkshopInventory.Services;

namespace WorkshopInventory.Views
{
    public class StockByLocationRow
    {
        public string MaterialName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public partial class ReportsView : UserControl
    {
        private AppDbContext _db = null!;
        private StockService _stock = null!;

        public ReportsView()
        {
            InitializeComponent();
        }

        public void Initialize(AppDbContext db, StockService stock, User user)
        {
            _db = db;
            _stock = stock;
            Refresh();
        }

        public void Refresh()
        {
            if (_db == null) return;

            var materials = _db.Materials.OrderBy(m => m.Name).ToList();

            StockGrid.ItemsSource = materials.Select(m => new MaterialRow
            {
                Id = m.Id,
                Name = m.Name,
                Category = m.Category,
                Unit = m.Unit,
                MinQuantity = m.MinQuantity,
                Quantity = _stock.GetTotalQuantity(m.Id),
                Location = MaterialsView.AllLocations
            }).ToList();

            StockByLocationGrid.ItemsSource = _db.MaterialStocks
                .Where(s => s.Quantity != 0)
                .OrderBy(s => s.Material!.Name).ThenBy(s => s.Location)
                .Select(s => new StockByLocationRow
                {
                    MaterialName = s.Material!.Name,
                    Location = s.Location,
                    Quantity = s.Quantity,
                    Unit = s.Material.Unit
                }).ToList();

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

            MovementGrid.ItemsSource = rows.OrderByDescending(r => r.Date).ToList();
        }
    }
}
