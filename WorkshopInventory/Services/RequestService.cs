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

        // Мастер: "Создание заявки на получение детали"
        public MaterialRequest CreateRequest(int masterId, int materialId, decimal quantity, RequestType type, string comment)
        {
            var request = new MaterialRequest
            {
                MasterId = masterId,
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

        // Руководитель: "Просмотр заявки и отправка кладовщику"
        public void Approve(int requestId)
        {
            var request = _db.Requests.Find(requestId) ?? throw new InvalidOperationException("Заявка не найдена");
            request.Status = RequestStatus.Одобрена;
            request.DateProcessed = DateTime.Now;
            _db.SaveChanges();
        }

        public void Reject(int requestId, string reason)
        {
            var request = _db.Requests.Find(requestId) ?? throw new InvalidOperationException("Заявка не найдена");
            request.Status = RequestStatus.Отклонена;
            request.Comment = reason;
            request.DateProcessed = DateTime.Now;
            _db.SaveChanges();
        }

        // Кладовщик: "Просмотр остатков" -> ромб "В наличии?" -> выдача/отказ (см. activity-диаграмму)
        // Возвращает true, если материал выдан/принят, false — если отклонено из-за нехватки.
        public bool Fulfill(int requestId, int storekeeperId)
        {
            var request = _db.Requests.Find(requestId) ?? throw new InvalidOperationException("Заявка не найдена");
            var material = _db.Materials.Find(request.MaterialId) ?? throw new InvalidOperationException("Материал не найден");

            if (request.Type == RequestType.Получение)
            {
                // Заявки мастеров закрываются с основного склада — если материал лежит
                // только на другом складе, кладовщик сначала должен переместить его на
                // основной (вкладка "Складские операции" → "Перемещение").
                var available = _stock.GetQuantityAt(material.Id, StockService.DefaultLocation);
                if (available < request.Quantity)
                {
                    // Ветка "Нет" на диаграмме — уведомление мастеру и завершение без выдачи
                    request.Status = RequestStatus.Отклонена;
                    request.Comment = "Недостаточно материала на складе";
                    request.DateProcessed = DateTime.Now;
                    _db.SaveChanges();
                    return false;
                }

                // Ветка "Да" — формирование отчёта о наличии/получении и списание со склада
                _stock.RegisterWriteOff(material.Id, request.Quantity, $"Выдача по заявке №{request.Id}", storekeeperId);
            }
            else
            {
                // Возврат материалов мастером на склад
                _stock.RegisterReceipt(material.Id, request.Quantity, $"Возврат по заявке №{request.Id}", storekeeperId);
            }

            request.Status = RequestStatus.Выполнена;
            request.DateProcessed = DateTime.Now;
            _db.SaveChanges();
            return true;
        }
    }
}
