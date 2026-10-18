using System.Security.Cryptography;
using System.Text;

namespace AutoParts.Core
{
    public sealed class Area
    {
        public string Key { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Page { get; init; } = string.Empty;
    }

    public static class Access
    {
        public static readonly Area[] Areas =
        {
            new Area { Key = "products", Name = "Products", Page = "AdminProducts" },
            new Area { Key = "quotes", Name = "Quotes & inquiries", Page = "AdminQuotes" },
            new Area { Key = "dealers", Name = "Dealers", Page = "AdminDealers" },
            new Area { Key = "news", Name = "News", Page = "AdminNews" },
            new Area { Key = "careers", Name = "Careers", Page = "AdminCareers" },
            new Area { Key = "pages", Name = "Pages", Page = "AdminPages" },
            new Area { Key = "media", Name = "Media", Page = "AdminMedia" },
            new Area { Key = "settings", Name = "Settings", Page = "AdminSettings" },
            new Area { Key = "activity", Name = "Activity log", Page = "AdminActivity" }
        };

        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] dk = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 100000, HashAlgorithmName.SHA256, 32);
            return "pbkdf2$100000$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(dk);
        }

        public static bool Verify(string password, string stored)
        {
            string[] parts = (stored ?? string.Empty).Split('$');
            if (parts.Length != 4 || parts[0] != "pbkdf2" || !int.TryParse(parts[1], out int iter) || iter < 10000 || iter > 1000000)
            {
                return false;
            }
            try
            {
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] want = Convert.FromBase64String(parts[3]);
                byte[] got = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iter, HashAlgorithmName.SHA256, want.Length);
                return CryptographicOperations.FixedTimeEquals(got, want);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
