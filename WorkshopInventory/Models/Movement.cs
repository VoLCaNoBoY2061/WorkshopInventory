namespace WorkshopInventory.Models
{
    public class Movement
    {
        public int Id { get; set; }

        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        public decimal Quantity { get; set; }
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;

        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
