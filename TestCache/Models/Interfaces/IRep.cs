using System;
using System.Threading.Tasks;

namespace TestCache.Models.Interfaces
{
    public interface IRep
    {
        public void AddCard(Card card);

        public int CountCards();

        public Task<Card> GetCard(string number);

        public Task<int> CountAttempts(string num);

        public Task<string> AddAttempt(string num);

        public Task<Attempt> GetAttempt(string guid);

        public Task UpdateAttempt(Attempt at);

        public Task DeleteAttempt(Attempt at);

        public Task LockCard(string num);

        public Task UnlockCard(string num);

        public Task BlockCard(string num);

        public Task<double?> GetBalance(string num);

        public Task<Tuple<bool, double, DateTime>> TakeMoney(string num, double sum);
    }
}
