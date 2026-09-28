namespace WorkshopInventory.Models
{
    public class Receipt
    {
        public int Id { get; set; }

        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        public decimal Quantity { get; set; }
        public string Supplier { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;

        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
