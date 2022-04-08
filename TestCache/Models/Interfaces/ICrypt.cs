
namespace TestCache.Models.Interfaces
{
    public interface ICrypt
    {
        public string blend(string pin, string num);

        public string HashPin(string pin, string num);

        public bool Compare(string pin, string num, string hash);
    }
}
