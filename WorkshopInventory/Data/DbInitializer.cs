using WorkshopInventory.Models;
using WorkshopInventory.Services;

namespace WorkshopInventory.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext db)
        {
            db.Database.EnsureCreated();

            if (!db.Users.Any())
            {
                db.Users.AddRange(
                    new User { FullName = "Иванов И.И.", Login = "storekeeper", PasswordHash = AuthService.Hash("1234"), Role = UserRole.Кладовщик },
                    new User { FullName = "Петров П.П.", Login = "manager", PasswordHash = AuthService.Hash("1234"), Role = UserRole.Руководитель },
                    new User { FullName = "Сидоров С.С.", Login = "master", PasswordHash = AuthService.Hash("1234"), Role = UserRole.Мастер }
                );
            }

            if (!db.Materials.Any())
            {
                var bolt = new Material { Name = "Болт М8", Unit = "шт", Category = "Крепёж", MinQuantity = 100 };
                var nut = new Material { Name = "Гайка М8", Unit = "шт", Category = "Крепёж", MinQuantity = 100 };
                var sheet = new Material { Name = "Лист стальной 2мм", Unit = "м2", Category = "Металл", MinQuantity = 10 };
                var paint = new Material { Name = "Краска эмаль", Unit = "л", Category = "Расходники", MinQuantity = 5 };

                db.Materials.AddRange(bolt, nut, sheet, paint);
                db.SaveChanges(); // нужно сохранить, чтобы получить Id материалов для остатков

                db.MaterialStocks.AddRange(
                    new MaterialStock { MaterialId = bolt.Id, Location = StockService.DefaultLocation, Quantity = 500 },
                    new MaterialStock { MaterialId = nut.Id, Location = StockService.DefaultLocation, Quantity = 480 },
                    new MaterialStock { MaterialId = sheet.Id, Location = StockService.DefaultLocation, Quantity = 25 },
                    // Немного краски специально положим на второй склад — чтобы сразу видна
                    // была многосклад ность после первого запуска
                    new MaterialStock { MaterialId = paint.Id, Location = StockService.DefaultLocation, Quantity = 5 },
                    new MaterialStock { MaterialId = paint.Id, Location = "Малярный участок", Quantity = 3 }
                );
            }

            db.SaveChanges();
        }
    }
}
