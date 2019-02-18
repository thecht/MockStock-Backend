using Microsoft.EntityFrameworkCore;

namespace MockStockBackend.DataModels
{
    public class ApplicationDbContext: DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<League> Leagues { get; set; }
        public DbSet<LeagueUser> LeagueUsers { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<LeagueUser>().HasKey(table => new {
                table.UserId, table.LeagueId
            });
        }
        
    }
}