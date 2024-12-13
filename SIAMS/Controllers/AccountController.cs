using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SIAMS.Data;
using SIAMS.Models;
using SIAMS.Services;


namespace SIAMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;




        public AccountController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }




        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private bool IsValidPassword(string password)
        {
            return password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => "!@#$%^&*()_+|<>?".Contains(ch));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!IsValidPassword(model.Password))
            {
                ViewData["Feedback"] = "Password does not meet security requirements.";
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ViewData["Feedback"] = "The username is already taken. Please choose a different one.";
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ViewData["Feedback"] = "The email is already registered. Please use a different email.";
                return View(model);
            }

            // Generate token for email verification
            var emailConfirmationToken = Guid.NewGuid().ToString();

            var hashedPassword = HashPasswordArgon2(model.Password);

            var user = new User
            {
                Username = model.Username,
                PasswordHash = hashedPassword,
                Email = model.Email,
                Role = "User",
                IsEmailConfirmed = false,
                EmailConfirmationToken = emailConfirmationToken
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send confirmation email
            var confirmationLink = Url.Action(
                "ConfirmEmail", "Account",
                new { token = emailConfirmationToken, email = user.Email },
                Request.Scheme);

            await _emailService.SendEmailAsync(user.Email, "Confirm Your Email",
                $"<p>Please confirm your email by clicking <a href='{confirmationLink}'>here</a>.</p>");

            ViewData["Feedback"] = "Registration successful! Please check your email to confirm your account.";
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return BadRequest("Invalid email confirmation request.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.EmailConfirmationToken == token);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = null; // Clear token
            await _context.SaveChangesAsync();

            TempData["Message"] = "Email confirmed successfully!";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();  // Renders the Register.cshtml view
        }

        // Display Login Page (GET)
        [HttpGet]
        public IActionResult Login()
        {
            return View();  // Renders the Login.cshtml view
        }

        // Handle Login Form Submission (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (user == null || HashPasswordArgon2(model.Password) != user.PasswordHash)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            if (!user.IsEmailConfirmed)
            {
                ModelState.AddModelError("", "Please confirm your email before logging in.");
                return View(model);
            }

            // Create Authentication Cookie
            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.Name, user.Username),
                new(System.Security.Claims.ClaimTypes.Role, user.Role)
            };

            var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);


            return RedirectToAction("Index", "Home");
        }

        // Logout Logic
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // Password Hashing Helper Method
        private static string HashPasswordArgon2(string password)
        {
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));
            argon2.Salt = Encoding.UTF8.GetBytes("YourSecureSaltValueHere");
            argon2.DegreeOfParallelism = 8;   // Number of threads
            argon2.MemorySize = 65536;        // Memory in KB (64MB)
            argon2.Iterations = 4;            // Number of passes

            var hashBytes = argon2.GetBytes(32);  // 32-byte hash
            return Convert.ToBase64String(hashBytes);
        }
    }
}
