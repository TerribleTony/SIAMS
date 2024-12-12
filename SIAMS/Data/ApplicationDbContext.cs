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

    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context)
        {
            // Check if users already exist
            if (context.Users.Any()) return;

            // Add default users
            context.Users.AddRange(
                new User { Username = "admin", PasswordHash = "adminhashed", Role = "Admin" },
                new User { Username = "user1", PasswordHash = "user1hashed", Role = "User" },
                new User { Username = "user2", PasswordHash = "user2hashed", Role = "User" }
            );

            context.SaveChanges();
        }
    }
}

