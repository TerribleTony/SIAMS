using Konscious.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SIAMS.Models;
using System.Text;

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
            // Ensure the database is created
            context.Database.Migrate();

            // Check if users already exist
            if (context.Users.Any()) return;

            // Add default users
            context.Users.AddRange(
                new User
                {
                    Username = "admin1",
                    PasswordHash = HashPassword("Admin1SecurePassword", "kljasedefkjnbsdkhsef"),
                    Salt = "RandomSaltValue1",
                    Role = "Admin",
                    Email = "admin1@example.com",
                    IsEmailConfirmed = true
                },
                new User
                {
                    Username = "admin2",
                    PasswordHash = HashPassword("Admin2SecurePassword", "kljasedefkjnbsdkhsef"),
                    Salt = "RandomSaltValue2",
                    Role = "Admin",
                    Email = "admin2@example.com",
                    IsEmailConfirmed = true
                }
            );

            context.SaveChanges();
        }

        // Password Hashing Method
        private static string HashPassword(string password, string salt)
        {
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));
            argon2.Salt = Encoding.UTF8.GetBytes(salt);
            argon2.DegreeOfParallelism = 8;   // Number of threads
            argon2.MemorySize = 65536;        // Memory in KB (64MB)
            argon2.Iterations = 4;            // Number of passes

            var hashBytes = argon2.GetBytes(32);  // 32-byte hash
            return Convert.ToBase64String(hashBytes);
        }
    }
}
