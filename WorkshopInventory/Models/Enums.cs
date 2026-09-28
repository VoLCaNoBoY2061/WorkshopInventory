namespace WorkshopInventory.Models
{
    public enum UserRole
    {
        Кладовщик,
        Руководитель,
        Мастер
    }

    public enum RequestType
    {
        Получение,
        Возврат
    }

    public enum RequestStatus
    {
        Новая,
        Одобрена,
        Отклонена,
        Выполнена
    }
}
