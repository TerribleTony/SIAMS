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

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(User model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null) return NotFound();

            user.Username = model.Username;
            user.Email = model.Email;
            user.Role = model.Role;

            _context.Update(user);
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
            await _context.SaveChangesAsync();

            TempData["Success"] = "User deleted successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ApproveAdminRequest(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.IsAdminRequested)
            {
                user.Role = "Admin";
                user.IsAdminRequested = false;  // Reset the request
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        public bool HasRequestedAdmin(int userId)
        {
            return _context.Logs.Any(l => l.Action == "RequestAdmin" && l.UserId == userId);
        }

    }
}
