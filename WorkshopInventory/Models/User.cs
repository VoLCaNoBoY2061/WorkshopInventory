using System.ComponentModel.DataAnnotations;

namespace WorkshopInventory.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Login { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; }
    }
}
