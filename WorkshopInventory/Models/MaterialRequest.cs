namespace WorkshopInventory.Models
{
    public class MaterialRequest
    {
        public int Id { get; set; }

        public int MasterId { get; set; }
        public User? Master { get; set; }

        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        public decimal Quantity { get; set; }
        public RequestType Type { get; set; } = RequestType.Получение;
        public RequestStatus Status { get; set; } = RequestStatus.Новая;

        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime? DateProcessed { get; set; }

        public string Comment { get; set; } = string.Empty;
    }
}
