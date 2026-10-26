using System;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using shop_pc.db;

namespace shop_pc.services
{
    public static class RoleNames
    {
        public const string Admin = "Админ";
        public const string Seller = "Продавец";
        public const string Storekeeper = "Склад";
    }

    public static class AuthService
    {
        public static User CurrentUser { get; private set; }

        public static bool IsAdmin
        {
            get { return CurrentUser.Role.RoleName == RoleNames.Admin; }
        }
        public static bool CanSell
        {
            get { return IsAdmin || CurrentUser.Role.RoleName == RoleNames.Seller; }
        }
        public static bool CanEditCatalog
        {
            get { return IsAdmin || CurrentUser.Role.RoleName == RoleNames.Storekeeper; }
        }

        public static string Hash(string password)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "");
            }
        }

        public static bool Login(string login, string password)
        {
            using (var db = new shop_pcEntities())
            {
                User user = db.Users
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.Login == login && u.IsActive);

                if (user == null) return false;
                if (!string.Equals(user.PasswordHash, Hash(password),
                                   StringComparison.OrdinalIgnoreCase)) return false;

                CurrentUser = user;
                return true;
            }
        }

        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}