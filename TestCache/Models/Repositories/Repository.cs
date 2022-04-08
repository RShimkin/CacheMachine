using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TestCache.Models.Interfaces;

namespace TestCache.Models.Repositories
{
    enum OpCode
    {
        BALANCE = 1,
        TAKE = 2
    }
    public class Repository : IRep
    {
        private readonly string _constr;

        static Repository()
        {
            /* using (AppDbContext db = new())
            {
                cardCount = db.Cards.Count();
            } */
        }

        public void AddCard(Card card)
        {
            using (AppDbContext db = new(_constr))
            {
                db.Cards.Add(card);
                db.SaveChanges();
            }
        }

        public Repository(string connectionString)
        {
            _constr = connectionString;
        }

        public int CountCards()
        {
            int res;
            using (AppDbContext db = new(_constr))
            {
                res = db.Cards.Count();
            }
            return res;
        }

        async public Task<Card> GetCard(string number)
        {
            Card card;
            using (var db = new AppDbContext(_constr))
            {
                card = await db.Cards.Where(x => x.Number == number).Include(x => x.Info).FirstOrDefaultAsync();
            }
            return card;
        }

        async public Task<int> CountAttempts(string num)
        {
            int res;
            using (var db = new AppDbContext(_constr))
            {
                var arr = db.Attempts.Where(x => x.Number == num).Where(x => x.Start < (DateTime.Now.AddMinutes(-1))).ToList();
                db.Attempts.RemoveRange(arr);
                await db.SaveChangesAsync();
                res = await db.Attempts.Where(x => x.Number == num).CountAsync();
            }
            return res;
        }

        async public Task<string> AddAttempt(string num)
        {
            var guid = Guid.NewGuid().ToString();
            Attempt attempt = new()
            {
                Id = guid,
                Counter = 4,
                Start = DateTime.Now,
                Number = num
            };
            using (var db = new AppDbContext(_constr))
            {
                db.Attempts.Add(attempt);
                await db.SaveChangesAsync();
            }
            return guid;
        }

        async public Task<Attempt> GetAttempt(string guid)
        {
            Attempt attempt;
            using (var db = new AppDbContext(_constr))
            {
                attempt = await db.Attempts.FindAsync(guid);
            }
            return attempt;
        }

        async public Task UpdateAttempt(Attempt at)
        {
            using (var db = new AppDbContext(_constr))
            {
                Attempt attempt = await db.Attempts.FindAsync(at.Id);
                attempt.Counter = at.Counter;
                await db.SaveChangesAsync();
            }
        }

        async public Task DeleteAttempt(Attempt at)
        {
            using (var db = new AppDbContext(_constr))
            {
                db.Attempts.Remove(at);
                await db.SaveChangesAsync();
            }
        }

        async public Task LockCard(string num)
        {
            using (var db = new AppDbContext(_constr))
            {
                Card card = await db.Cards.Where(x => x.Number == num).Include(x => x.Info).FirstOrDefaultAsync();
                if (card != null && card.Info != null)
                {
                    card.Info.Locked = true;
                    await db.SaveChangesAsync();
                }
            }
        }

        async public Task UnlockCard(string num)
        {
            using (var db = new AppDbContext(_constr))
            {
                Card card = await db.Cards.Where(x => x.Number == num).Include(x => x.Info).FirstOrDefaultAsync();
                if (card != null && card.Info != null)
                {
                    card.Info.Locked = false;
                    await db.SaveChangesAsync();
                }
            }
        }

        async public Task BlockCard(string num)
        {
            using (var db = new AppDbContext(_constr))
            {
                Card card = await db.Cards.Where(x => x.Number == num).Include(x => x.Info).FirstOrDefaultAsync();
                if (card != null && card.Info != null)
                {
                    card.Info.Blocked = true;
                    await db.SaveChangesAsync();
                }
            }
        }

        async public Task<double?> GetBalance(string num)
        {
            double? balance = null;
            using (var db = new AppDbContext(_constr))
            {
                Card card = await db.Cards.Where(x => x.Number == num).Include(x => x.Info).FirstOrDefaultAsync();
                if (card != null && card.Info != null)
                {
                    await Journal(num, (int)OpCode.BALANCE);
                    balance = card.Info.Balance;
                }
            }
            return balance;
        }

        async public Task<Tuple<bool,double,DateTime>> TakeMoney(string num, double sum)
        {
            double balance = 0;
            double residue = 0;
            bool result = false;
            DateTime dt = DateTime.Today;
            using (var db = new AppDbContext(_constr))
            {
                Card card = await db.Cards.Where(x => x.Number == num).Include(x => x.Info).FirstOrDefaultAsync();
                if (card != null && card.Info != null)
                {
                    balance = card.Info.Balance;
                    if (balance >= sum)
                    {
                        using (var transaction = db.Database.BeginTransaction())
                        {
                            try
                            {
                                await Journal(num, (int)OpCode.TAKE, sum);
                                card.Info.Balance -= sum;
                                db.SaveChanges();
                                transaction.Commit();
                                result = true;
                                residue = balance - sum;
                                dt = DateTime.Now;
                            }
                            catch
                            {
                                transaction.Rollback();
                            }
                        }
                    }
                    else
                    {
                        residue = balance - sum;
                    }
                    await Journal(num, (int)OpCode.TAKE);
                }
            }
            return (result, residue, dt).ToTuple();
        }

        async private Task Journal(string number, int opcode, double sum = 0)
        {
            Operation op = new()
            {
                OperationCode = opcode,
                CardNumber = number,
            };
            using (var db = new AppDbContext(_constr))
            {
                db.Operations.Add(op);
                await db.SaveChangesAsync();
            }
        }
    }
}
