using Microsoft.EntityFrameworkCore;
using System;

namespace TestCache.Models
{
    public class AppDbContext : DbContext
    {
        private readonly string _constr;
        public DbSet<Card> Cards { get; set; } = null!;

        public DbSet<LogMessages> Log { get; set; } = null!;

        public DbSet<Operation> Operations { get; set; } = null!;

        public DbSet<Attempt> Attempts { get; set; } = null!;

        public AppDbContext(string connectionString = "Server=localhost;port=3306;Database=db3;uid=root;password=12022000Roma")
        {
            _constr = connectionString;
            //Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(_constr, new MySqlServerVersion(new Version(8, 0, 18)));
            optionsBuilder.LogTo(message => System.Diagnostics.Debug.WriteLine(message));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /*
            modelBuilder.Entity<Card>().HasData();
            modelBuilder.Entity<Card>().HasData();
            */
        }
    }
}
