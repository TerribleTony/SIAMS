using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAMS.Data;
using SIAMS.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SIAMS.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /UserProfile
        public async Task<IActionResult> Index()
        {
            var username = User.Identity.Name;

            // Get user info
            var user = await _context.Users
                .Include(u => u.Assets)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return NotFound("User not found.");

            // Get last 5 logs for the user
            var recentLogs = await _context.Logs
                .Where(log => log.PerformedBy == username)
                .OrderByDescending(log => log.Timestamp)
                .Take(5)
                .ToListAsync();

            // Pass data to view
            var model = new UserProfileViewModel
            {
                User = user,
                RecentLogs = recentLogs
            };

            return View(model);
        }

        // GET: /UserProfile/Edit
        public async Task<IActionResult> Edit()
        {
            var username = User.Identity.Name;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound("User not found.");

            var model = new EditUserViewModel
            {
                Email = user.Email,
            };

            return View(model);
        }

        // POST: /UserProfile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var username = User.Identity.Name;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound("User not found.");

            // Update details
            user.Email = model.Email;
            await _context.SaveChangesAsync();

            ViewData["Message"] = "Profile updated successfully!";
            return RedirectToAction("Index");
        }

        // POST: /UserProfile/RequestAdmin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestAdmin()
        {
            var username = User.Identity?.Name;

            if (username == null)
            {
                TempData["Error"] = "You must be logged in to request admin access.";
                return RedirectToAction("Index", "Home");
            }

            // Find the current user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user != null && !user.IsAdminRequested)
            {
                // Update the user's admin request status
                user.IsAdminRequested = true;

                // Log the request
                _context.Logs.Add(new Log
                {
                    Action = $"{username} requested admin access.",
                    Timestamp = DateTime.UtcNow,
                    PerformedBy = username
                });

                await _context.SaveChangesAsync();

                TempData["Message"] = "Admin request submitted!";
            }
            else
            {
                TempData["Error"] = "Admin request could not be processed.";
            }

            return RedirectToAction("Index", "UserProfile");
        }

    }
}
