using System.ComponentModel.DataAnnotations;

namespace WorkshopInventory.Models
{
    /// <summary>Остаток конкретного материала на конкретном складе.
    /// Один материал может иметь несколько таких записей — по одной на каждый склад,
    /// где он физически присутствует.</summary>
    public class MaterialStock
    {
        public int Id { get; set; }

        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        [Required, MaxLength(100)]
        public string Location { get; set; } = "Основной склад";

        public decimal Quantity { get; set; }
    }
}
