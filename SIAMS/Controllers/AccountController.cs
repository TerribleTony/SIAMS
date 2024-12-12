using Konscious.Security.Cryptography;   // Hashing library
using System.Text;                      // Encoding support
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAMS.Data;
using SIAMS.Models;


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
        // Displays the login form (GET)
        [HttpGet]
        public IActionResult Login()
        {
            return View();  // Renders the Login.cshtml view
        }

        // Handles form submission (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            // Verify Password with Argon2
            var inputHash = HashPasswordArgon2(model.Password);
            if (inputHash != user.PasswordHash)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            // Login successful, redirect to home
            return RedirectToAction("Index", "Home");
        }

        // Helper Method for Argon2 Hashing
        private static string HashPasswordArgon2(string password)
        {
            // Use Argon2id for password hashing
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));

            // Configure Argon2 settings
            argon2.Salt = Encoding.UTF8.GetBytes("YourSecureSaltValueHere");
            argon2.DegreeOfParallelism = 8;   // Number of threads
            argon2.MemorySize = 65536;        // Memory in KB (64MB)
            argon2.Iterations = 4;            // Number of passes

            // Generate the hash
            var hashBytes = argon2.GetBytes(32);  // 32-byte hash
            return Convert.ToBase64String(hashBytes);
        }
    }
}
