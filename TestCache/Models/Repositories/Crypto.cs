using TestCache.Models.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace TestCache.Models.Repositories
{
    public class Crypto : ICrypt
    {
        public string blend(string pin, string num)
        {
            if (pin.Length == 4)
            {
                string salt = num[15] + num[4] + num[11] + num[0] + num[6..9];
                return pin[0..1] + salt + pin[2..3];
            }
            return "";
        }

        public string HashPin(string pin, string num)
        {
            return BC.HashPassword(blend(pin, num));
        }

        public bool Compare(string pin, string num, string hash)
        {
            return BC.Verify(blend(pin, num), hash);
        }
    }
}
