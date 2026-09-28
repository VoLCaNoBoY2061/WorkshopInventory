using System.ComponentModel.DataAnnotations;

namespace WorkshopInventory.Models
{
    // Материал больше не хранит остаток и склад напрямую — теперь у него может быть
    // несколько остатков на разных складах (см. MaterialStock). MinQuantity — это общий
    // порог дефицита по всем складам вместе.
    public class Material
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Unit { get; set; } = "шт";

        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        public decimal MinQuantity { get; set; }
    }
}
