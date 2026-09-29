using WorkshopInventory.Data;
using WorkshopInventory.Models;

namespace WorkshopInventory.Services
{
    public class StockService
    {
        public const string DefaultLocation = "Основной склад";

        private readonly AppDbContext _db;
        public StockService(AppDbContext db) => _db = db;

        public List<string> GetLocations()
        {
            var locations = _db.MaterialStocks
                .Select(s => s.Location)
                .Distinct()
                .OrderBy(l => l)
                .ToList();

            if (!locations.Contains(DefaultLocation))
                locations.Insert(0, DefaultLocation);

            return locations;
        }

        public decimal GetTotalQuantity(int materialId)
            => _db.MaterialStocks.Where(s => s.MaterialId == materialId)
                .Select(s => s.Quantity).ToList().Sum();

        public decimal GetQuantityAt(int materialId, string location)
            => _db.MaterialStocks.FirstOrDefault(s => s.MaterialId == materialId && s.Location == location)?.Quantity ?? 0;

        private MaterialStock GetOrCreateStock(int materialId, string location)
        {
            var stock = _db.MaterialStocks.FirstOrDefault(s => s.MaterialId == materialId && s.Location == location);
            if (stock == null)
            {
                stock = new MaterialStock { MaterialId = materialId, Location = location, Quantity = 0 };
                _db.MaterialStocks.Add(stock);
            }
            return stock;
        }

        public void RegisterReceipt(int materialId, decimal quantity, string supplier, User user, string location = DefaultLocation)
        {
            // ПРОВЕРКА РОЛИ
            if (user.Role != UserRole.Кладовщик)
                throw new InvalidOperationException("Только кладовщик может оприходовать материалы");

            _ = _db.Materials.Find(materialId) ?? throw new InvalidOperationException("Материал не найден");

            var stock = GetOrCreateStock(materialId, location);
            stock.Quantity += quantity;

            _db.Receipts.Add(new Receipt { MaterialId = materialId, Quantity = quantity, Supplier = supplier, UserId = user.Id, Location = location });
            _db.SaveChanges();
        }

        public void RegisterWriteOff(int materialId, decimal quantity, string reason, User user, string location = DefaultLocation)
        {
            // ПРОВЕРКА РОЛИ
            if (user.Role != UserRole.Кладовщик)
                throw new InvalidOperationException("Только кладовщик может списывать материалы");

            var material = _db.Materials.Find(materialId) ?? throw new InvalidOperationException("Материал не найден");
            var stock = GetOrCreateStock(materialId, location);

            if (stock.Quantity < quantity)
                throw new InvalidOperationException($"Недостаточно материала на складе «{location}». Остаток: {stock.Quantity} {material.Unit}");

            stock.Quantity -= quantity;
            _db.WriteOffs.Add(new WriteOff { MaterialId = materialId, Quantity = quantity, Reason = reason, UserId = user.Id, Location = location });
            _db.SaveChanges();
        }

        public void RegisterMovement(int materialId, decimal quantity, string from, string to, User user)
        {
            // ПРОВЕРКА РОЛИ
            if (user.Role != UserRole.Кладовщик)
                throw new InvalidOperationException("Только кладовщик может перемещать материалы между складами");

            var material = _db.Materials.Find(materialId) ?? throw new InvalidOperationException("Материал не найден");

            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Склад отправления и назначения совпадают");

            var fromStock = GetOrCreateStock(materialId, from);
            if (fromStock.Quantity < quantity)
                throw new InvalidOperationException($"На складе «{from}» недостаточно материала. Остаток: {fromStock.Quantity} {material.Unit}");

            var toStock = GetOrCreateStock(materialId, to);

            fromStock.Quantity -= quantity;
            toStock.Quantity += quantity;

            _db.Movements.Add(new Movement { MaterialId = materialId, Quantity = quantity, FromLocation = from, ToLocation = to, UserId = user.Id });
            _db.SaveChanges();
        }
    }
}