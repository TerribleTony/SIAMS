using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAMS.Data;
using SIAMS.Models;

namespace SIAMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserManagementController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<User?> GetCurrentUser()
        {
            var username = User?.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;

            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Log the view event
            var currentAdmin = await GetCurrentUser();
            if (currentAdmin != null)
            {
                _context.Logs.Add(new Log
                {
                    UserId = user.UserId,
                    Action = $"Viewed edit form for user '{user.Username}'.",
                    Timestamp = DateTime.UtcNow,
                    PerformedBy = currentAdmin.Username
                });
                await _context.SaveChangesAsync();
            }

            return View(user);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null) return NotFound();

            user.Username = model.Username;
            user.Email = model.Email;
            user.Role = model.Role;

            _context.Update(user);

            // Log the edit action
            var currentAdmin = await GetCurrentUser();
            if (currentAdmin != null)
            {
                _context.Logs.Add(new Log
                {
                    UserId = user.UserId,
                    Action = $"User '{user.Username}' updated by '{currentAdmin.Username}'.",
                    Timestamp = DateTime.UtcNow,
                    PerformedBy = currentAdmin.Username
                });
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "User updated successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            _context.Users.Remove(user);

            // Log the delete action
            var currentAdmin = await GetCurrentUser();
            if (currentAdmin != null)
            {
                _context.Logs.Add(new Log
                {
                    UserId = user.UserId,
                    Action = $"User '{user.Username}' deleted by '{currentAdmin.Username}'.",
                    Timestamp = DateTime.UtcNow,
                    PerformedBy = currentAdmin.Username
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "User deleted successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveAdminRequest(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            // Update the user's role
            user.Role = "Admin";
            user.IsAdminRequested = false;

            // Retrieve current admin performing the action
            var adminName = User?.Identity?.Name;
            var currentAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Username == adminName);

            if (currentAdmin == null)
            {
                TempData["Error"] = "Admin not recognized.";
                return RedirectToAction("Index");
            }

            // Log the approval action
            _context.Logs.Add(new Log
            {
                UserId = user.UserId,  // User being approved
                Action = $"User '{user.Username}' was approved for admin access by '{currentAdmin.Username}'.",
                Timestamp = DateTime.UtcNow,
                PerformedBy = currentAdmin.Username
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Admin request approved successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectAdminRequest(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            // Reset admin request status
            user.IsAdminRequested = false;

            // Retrieve the current admin
            var adminName = User?.Identity?.Name;
            var currentAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Username == adminName);

            if (currentAdmin == null)
            {
                TempData["Error"] = "Admin not recognized.";
                return RedirectToAction("Index");
            }

            // Log the rejection
            _context.Logs.Add(new Log
            {
                UserId = user.UserId,  // User whose request is rejected
                Action = $"User '{user.Username}' was rejected for admin access by '{currentAdmin.Username}'.",
                Timestamp = DateTime.UtcNow,
                PerformedBy = currentAdmin.Username
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Admin request rejected successfully.";
            return RedirectToAction("Index");
        }


        public bool HasRequestedAdmin(int userId)
        {
            return _context.Logs.Any(l => l.Action == "RequestAdmin" && l.UserId == userId);
        }



    }
}
