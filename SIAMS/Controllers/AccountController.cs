﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SIAMS.Data;
using SIAMS.Models;
using SIAMS.Services;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;  



namespace SIAMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;



        public AccountController(ApplicationDbContext context, IEmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        private void LogAction(string action, string? performedBy)
        {
            _context.Logs.Add(new Log
            {
                Action = action,
                Timestamp = DateTime.UtcNow,
                PerformedBy = performedBy ?? "System"
            });
            _context.SaveChanges();
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
                ModelState.AddModelError("Password", "Password does not meet security requirements.");
                LogAction("Failed registration attempt due to invalid password.", model.Username);
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == model.Username.ToLower()))
            {
                ModelState.AddModelError("Username", "The username is already taken.");
                LogAction($"Failed registration: Username '{model.Username}' already exists.", model.Username);
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower()))
            {
                ModelState.AddModelError("Email", "The email is already registered.");
                LogAction($"Failed registration: Email '{model.Email}' already exists.", model.Username);
                return View(model);
            }


            // Use environment variable in production, fallback to config during development
            var pepper = Environment.GetEnvironmentVariable("PEPPER") ??
                         _configuration["Security:Pepper"];  // Local fallback
            var emailConfirmationToken = Guid.NewGuid().ToString();
            var (passwordHash, salt) = HashPasswordArgon2(model.Password, pepper);


            var user = new User
            {
                Username = model.Username,
                PasswordHash = passwordHash,
                Salt = salt,
                Email = model.Email,
                Role = "User",
                IsEmailConfirmed = false,
                EmailConfirmationToken = emailConfirmationToken
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            LogAction($"New user registered: Username '{model.Username}', Email '{model.Email}'.", model.Username);

            if (!string.IsNullOrEmpty(emailConfirmationToken))
            {
                var confirmationLink = Url.Action(
                    "ConfirmEmail", "Account",
                    new { token = emailConfirmationToken, email = user.Email },
                    Request.Scheme);

                await _emailService.SendEmailAsync(user.Email, "Confirm Your Email",
                    $"<p>Please confirm your email by clicking <a href='{confirmationLink}'>here</a>.</p>");
            }

            TempData["Message"] = "Registration successful! Please check your email to confirm your account.";
            return RedirectToAction("Login", "Account");
        }




        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                LogAction("Invalid email confirmation request received.", email);
                return BadRequest("Invalid email confirmation request.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.EmailConfirmationToken == token);
            if (user == null)
            {
                LogAction("Email confirmation failed: User not found.", email);
                return NotFound("User not found.");
            }

            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = null; // Clear token
            await _context.SaveChangesAsync();

            LogAction($"Email confirmed successfully for user '{user.Username}'.", email);

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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username && !u.IsDeleted);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            // Retrieve Pepper
            string pepper = Environment.GetEnvironmentVariable("PEPPER") ?? _configuration["Security:Pepper"];

            // Verify Password (with Pepper)
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(model.Password + pepper));
            argon2.Salt = Convert.FromBase64String(user.Salt);
            argon2.DegreeOfParallelism = 8;
            argon2.MemorySize = 65536;
            argon2.Iterations = 4;

            string computedHash = Convert.ToBase64String(argon2.GetBytes(32));

            if (computedHash != user.PasswordHash)
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
        internal static (string Hash, string Salt) HashPasswordArgon2(string password, string pepper)
        {
            // Generate unique salt
            byte[] saltBytes = new byte[16];
            RandomNumberGenerator.Fill(saltBytes);
            string salt = Convert.ToBase64String(saltBytes);

            // Combine password + pepper
            string passwordWithPepper = password + pepper;

            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(passwordWithPepper));
            argon2.Salt = saltBytes;
            argon2.DegreeOfParallelism = 8;  // Threads
            argon2.MemorySize = 65536;       // Memory in KB (64MB)
            argon2.Iterations = 4;           // Number of passes

            var hashBytes = argon2.GetBytes(32);  // 32-byte hash
            return (Convert.ToBase64String(hashBytes), salt);
        }

    }
}
