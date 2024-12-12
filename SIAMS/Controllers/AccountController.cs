﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Konscious.Security.Cryptography;
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

        // Registration Logic
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("", "Username already exists.");
                return View(model);
            }

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
