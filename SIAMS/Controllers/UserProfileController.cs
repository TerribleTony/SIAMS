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
            if (HttpContext?.User == null || HttpContext.User.Identity?.IsAuthenticated == false)
                return Unauthorized("User is not logged in.");

            var username = HttpContext.User.Identity?.Name;

            if (string.IsNullOrEmpty(username))
                return Unauthorized("User is not logged in.");

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
            if (HttpContext?.User == null || HttpContext.User.Identity?.IsAuthenticated == false)
                return Unauthorized("User is not logged in.");

            var username = User?.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized("User is not logged in.");

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



            var username = User?.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized("User is not logged in.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username!);
            if (user == null)
            {
               return NotFound("User not found.");
            }


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
            var username = User?.Identity?.Name;

            if (string.IsNullOrEmpty(username))
            {
                TempData["Error"] = "You must be logged in to request admin access.";
                return RedirectToAction("Index", "Home");
            }

            // Find the current user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                TempData["Error"] = "User not recognized.";
                return RedirectToAction("Index", "Home");
            }

            if (!user.IsAdminRequested)
            {
                // Update the user's admin request status
                user.IsAdminRequested = true;

                // Log the request with UserId
                _context.Logs.Add(new Log
                {
                    UserId = user.UserId,   // Correct User ID
                    Action = $"User '{user.Username}' requested admin access.",
                    Timestamp = DateTime.UtcNow,
                    PerformedBy = user.Username  // For backward compatibility
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
