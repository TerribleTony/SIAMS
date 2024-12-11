using Microsoft.EntityFrameworkCore;
using SIAMS.Models;

namespace SIAMS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Log> Logs { get; set; }
    }
}

