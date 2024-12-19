using Konscious.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SIAMS.Models;
using System.Security.Cryptography;
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
        public static void Seed(ApplicationDbContext context, IConfiguration configuration)
        {
            // Ensure the database is created
            context.Database.Migrate();

            if (context.Users.Any(u => u.Username == "admin1")) return;

            string admin1Password = GetEnvVarOrDefault("ADMIN1_PASSWORD", "DefaultSecurePassword1!");
            string admin2Password = GetEnvVarOrDefault("ADMIN2_PASSWORD", "DefaultSecurePassword2!");

            string admin1Salt = GenerateSalt();
            string admin2Salt = GenerateSalt();

            context.Users.AddRange(
                new User
                {
                    Username = "admin1",
                    PasswordHash = HashPassword(admin1Password, admin1Salt),
                    Salt = admin1Salt,
                    Role = "Admin",
                    Email = "admin1@example.com",
                    IsEmailConfirmed = true
                },
                new User
                {
                    Username = "admin2",
                    PasswordHash = HashPassword(admin2Password, admin2Salt),
                    Salt = admin2Salt,
                    Role = "Admin",
                    Email = "admin2@example.com",
                    IsEmailConfirmed = true
                }
            );

            context.SaveChanges();
        }

        private static string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            RandomNumberGenerator.Fill(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        private static string HashPassword(string password, string salt)
        {
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));
            argon2.Salt = Encoding.UTF8.GetBytes(salt);
            argon2.DegreeOfParallelism = 8;   // Number of threads
            argon2.MemorySize = 131072;        // Memory in KB (128MB)
            argon2.Iterations = 6;            // Number of passes

            var hashBytes = argon2.GetBytes(32);  // 32-byte hash
            return Convert.ToBase64String(hashBytes);
        }

        private static string GetEnvVarOrDefault(string key, string defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(value))
            {
                Console.WriteLine($"Warning: Environment variable '{key}' not set. Using default value.");
            }
            return value ?? defaultValue;
        }
    }
}
