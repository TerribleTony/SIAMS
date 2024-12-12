using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SIAMS.Data;
using SIAMS.Models;
using System.Security.Cryptography;
using System.Text;

namespace SIAMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Registration Action
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("", "Username already exists.");
                return View(model);
            }

            // Retrieve the secure Pepper from configuration
            var pepper = _configuration["AppSecrets:Pepper"];

            // Generate Salt
            var salt = GenerateSalt();
            var passwordHash = HashPassword(model.Password, salt, pepper);

            // Save the user to the database
            var user = new User
            {
                Username = model.Username,
                PasswordHash = passwordHash,
                Salt = salt,
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login", "Account");
        }

        // Login Action
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

            // Retrieve Pepper for validation
            var pepper = _configuration["AppSecrets:Pepper"];
            var hashedInput = HashPassword(model.Password, user.Salt, pepper);

            if (hashedInput != user.PasswordHash)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            // Sign in logic here (not shown)
            return RedirectToAction("Index", "Home");
        }

        // Helper Methods
        private static string GenerateSalt()
        {
            var saltBytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        private static string HashPassword(string password, string salt, string pepper)
        {
            using var sha256 = SHA256.Create();
            var saltedPassword = $"{salt}{password}{pepper}";
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(hashBytes);
        }

        // Login Page (GET) - Displays the login form
        [HttpGet]
        public IActionResult Login()
        {
            return View();  // Return the Login.cshtml view
        }
    }
}
