using WorkshopInventory.Data;
using WorkshopInventory.Models;

namespace WorkshopInventory.Services
{
    public class RequestService
    {
        private readonly AppDbContext _db;
        private readonly StockService _stock;

        public RequestService(AppDbContext db, StockService stock)
        {
            _db = db;
            _stock = stock;
        }

        public MaterialRequest CreateRequest(User master, int materialId, decimal quantity, RequestType type, string comment)
        {
            // ПРОВЕРКА РОЛИ
            if (master.Role != UserRole.Мастер)
                throw new InvalidOperationException("Только мастер может создавать заявки");

            var request = new MaterialRequest
            {
                MasterId = master.Id,
                MaterialId = materialId,
                Quantity = quantity,
                Type = type,
                Comment = comment,
                Status = RequestStatus.Новая
            };
            _db.Requests.Add(request);
            _db.SaveChanges();
            return request;
        }

        public void Approve(int requestId, User manager)
        {
            // ПРОВЕРКА РОЛИ
            if (manager.Role != UserRole.Руководитель)
                throw new InvalidOperationException("Только руководитель может одобрять заявки");

            var request = _db.Requests.Find(requestId) ?? throw new InvalidOperationException("Заявка не найдена");
            request.Status = RequestStatus.Одобрена;
            request.DateProcessed = DateTime.Now;
            _db.SaveChanges();
        }

        public void Reject(int requestId, string reason, User manager)
        {
            // ПРОВЕРКА РОЛИ
            if (manager.Role != UserRole.Руководитель)
                throw new InvalidOperationException("Только руководитель может отклонять заявки");

            var request = _db.Requests.Find(requestId) ?? throw new InvalidOperationException("Заявка не найдена");
            request.Status = RequestStatus.Отклонена;
            request.Comment = reason;
            request.DateProcessed = DateTime.Now;
            _db.SaveChanges();
        }

        public bool Fulfill(int requestId, User storekeeper)
        {
            // ПРОВЕРКА РОЛИ
            if (storekeeper.Role != UserRole.Кладовщик)
                throw new InvalidOperationException("Только кладовщик может выдавать материалы по заявкам");

            var request = _db.Requests.Find(requestId) ?? throw new InvalidOperationException("Заявка не найдена");
            var material = _db.Materials.Find(request.MaterialId) ?? throw new InvalidOperationException("Материал не найден");

            if (request.Type == RequestType.Получение)
            {
                var available = _stock.GetQuantityAt(material.Id, StockService.DefaultLocation);
                if (available < request.Quantity)
                {
                    request.Status = RequestStatus.Отклонена;
                    request.Comment = "Недостаточно материала на складе";
                    request.DateProcessed = DateTime.Now;
                    _db.SaveChanges();
                    return false;
                }

                _stock.RegisterWriteOff(material.Id, request.Quantity, $"Выдача по заявке №{request.Id}", storekeeper);
            }
            else
            {
                _stock.RegisterReceipt(material.Id, request.Quantity, $"Возврат по заявке №{request.Id}", storekeeper);
            }

            request.Status = RequestStatus.Выполнена;
            request.DateProcessed = DateTime.Now;
            _db.SaveChanges();
            return true;
        }
    }
}