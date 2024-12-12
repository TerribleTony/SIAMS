using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAMS.Data;
using SIAMS.Models;
using System.Text;

namespace SIAMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("", "Username already exists.");
                return View(model);
            }

            // Generate Argon2 Hash
            var hashedPassword = HashPasswordArgon2(model.Password);

            var user = new User
            {
                Username = model.Username,
                PasswordHash = hashedPassword,
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            var inputHash = HashPasswordArgon2(model.Password);

            if (inputHash != user.PasswordHash)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            // Redirect to Home on success
            return RedirectToAction("Index", "Home");
        }

        // Helper Method for Argon2 Hashing
        private static string HashPasswordArgon2(string password)
        {
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));

            // Argon2 Parameters (Recommended settings for security)
            argon2.Salt = Encoding.UTF8.GetBytes("YourSecureSaltValueHere");
            argon2.DegreeOfParallelism = 8;   // Multithreading
            argon2.MemorySize = 65536;       // 64 MB
            argon2.Iterations = 4;           // Number of passes

            var hashBytes = argon2.GetBytes(32); // Output size
            return Convert.ToBase64String(hashBytes);
        }
    }
}
