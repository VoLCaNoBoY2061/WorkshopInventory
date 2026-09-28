using System.Windows.Controls;
using WorkshopInventory.Data;
using WorkshopInventory.Models;
using WorkshopInventory.Services;
using System.Windows;
using Microsoft.Win32;
using ClosedXML.Excel;

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
                Date = r.Date,
                Type = "Поступление",
                MaterialName = r.Material!.Name,
                Quantity = r.Quantity,
                Details = $"{r.Supplier} → {r.Location}"
            }));
            rows.AddRange(_db.WriteOffs.Select(w => new OperationRow
            {
                Date = w.Date,
                Type = "Списание",
                MaterialName = w.Material!.Name,
                Quantity = w.Quantity,
                Details = $"{w.Reason} ({w.Location})"
            }));
            rows.AddRange(_db.Movements.Select(m => new OperationRow
            {
                Date = m.Date,
                Type = "Перемещение",
                MaterialName = m.Material!.Name,
                Quantity = m.Quantity,
                Details = $"{m.FromLocation} → {m.ToLocation}"
            }));

            MovementGrid.ItemsSource = rows.OrderByDescending(r => r.Date).ToList();
        }

        // ================= ЭКСПОРТ В EXCEL =================

        private void OnExportStockClick(object sender, RoutedEventArgs e)
        {
            ExportGridToExcel(StockGrid, "Остатки_материалов");
        }

        private void OnExportStockByLocationClick(object sender, RoutedEventArgs e)
        {
            ExportGridToExcel(StockByLocationGrid, "Остатки_по_складам");
        }

        private void OnExportMovementClick(object sender, RoutedEventArgs e)
        {
            ExportGridToExcel(MovementGrid, "Движение_материалов");
        }

        private void ExportGridToExcel(DataGrid grid, string sheetName)
        {
            if (grid.ItemsSource == null)
            {
                MessageBox.Show("Нет данных для экспорта.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Файлы Excel (*.xlsx)|*.xlsx",
                FileName = $"{sheetName}_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add(sheetName);

                        // Заголовки колонок
                        int colIndex = 1;
                        foreach (DataGridColumn column in grid.Columns)
                        {
                            if (column is DataGridTextColumn textColumn)
                            {
                                worksheet.Cell(1, colIndex).Value = textColumn.Header.ToString();
                                worksheet.Cell(1, colIndex).Style.Font.Bold = true;
                                worksheet.Cell(1, colIndex).Style.Fill.BackgroundColor = XLColor.LightGray;
                                colIndex++;
                            }
                        }

                        // Данные
                        int rowIndex = 2;
                        foreach (var item in grid.ItemsSource)
                        {
                            colIndex = 1;
                            foreach (DataGridColumn column in grid.Columns)
                            {
                                if (column is DataGridTextColumn textColumn)
                                {
                                    var bindingPath = textColumn.Binding.ToString().Split('{')[1].Split('}')[0];
                                    var propertyInfo = item.GetType().GetProperty(bindingPath);
                                    var value = propertyInfo?.GetValue(item)?.ToString() ?? "";

                                    worksheet.Cell(rowIndex, colIndex).Value = value;
                                    colIndex++;
                                }
                            }
                            rowIndex++;
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    MessageBox.Show("Отчет успешно сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}