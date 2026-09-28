using System.Security.Cryptography;
using System.Text;
using WorkshopInventory.Data;
using WorkshopInventory.Models;

namespace WorkshopInventory.Services
{
    public static class AuthService
    {
        public static string Hash(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public static User? Login(AppDbContext db, string login, string password)
        {
            var hash = Hash(password);
            return db.Users.FirstOrDefault(u => u.Login == login && u.PasswordHash == hash);
        }
    }
}
