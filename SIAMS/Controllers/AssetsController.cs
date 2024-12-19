using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIAMS.Data;
using SIAMS.Models;

namespace SIAMS.Controllers
{
    [Authorize] // Apply authorization to the entire controller
    public class AssetsController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Constructor
        public AssetsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Assets
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Assets.Include(a => a.AssignedUser);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Assets/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var asset = await _context.Assets
                .Include(a => a.AssignedUser)
                .FirstOrDefaultAsync(m => m.AssetId == id);
            if (asset == null)
            {
                return NotFound();
            }

            return View(asset);
        }

        // GET: Assets/Create
        public IActionResult Create()
        {
            ViewData["AssignedUserId"] = new SelectList(_context.Users, "UserId", "Username");
            return View();
        }

        // POST: Assets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AssetId,AssetName,Category,AssignedUserId")] Asset asset)
        {
            if (ModelState.IsValid)
            {
                // Add the asset to the database
                _context.Add(asset);

                // Retrieve the current user
                var username = User?.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    TempData["Error"] = "User not recognized.";
                    return RedirectToAction(nameof(Index));
                }

                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (currentUser == null)
                {
                    TempData["Error"] = "User not recognized.";
                    return RedirectToAction(nameof(Index));
                }

                // Log the asset creation action
                _context.Logs.Add(new Log
                {
                    UserId = currentUser.UserId,   // Correct User ID
                    Action = $"Asset '{asset.AssetName}' created.",
                    Timestamp = DateTime.UtcNow,
                    PerformedBy = currentUser.Username  // Correct username reference
                });

                await _context.SaveChangesAsync();
                TempData["Success"] = "Asset created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["AssignedUserId"] = new SelectList(_context.Users, "UserId", "Username", asset.AssignedUserId);
            return View(asset);
        }


        // GET: Assets/Edit
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Asset ID not provided.";
                return NotFound();
            }

            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                TempData["Error"] = "Asset not found.";
                return NotFound();
            }

            ViewData["AssignedUserId"] = new SelectList(_context.Users, "UserId", "Username", asset.AssignedUserId);
            return View(asset);
        }

        // POST: Assets/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AssetId,AssetName,Category,AssignedUserId")] Asset asset)
        {
            if (id != asset.AssetId)
            {
                TempData["Error"] = "Asset ID mismatch.";
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update asset
                    _context.Update(asset);

                    // Retrieve current user
                    var username = User?.Identity?.Name;
                    if (!string.IsNullOrEmpty(username))
                    {
                        var currentUser = await _context.Users
                            .FirstOrDefaultAsync(u => u.Username == username);

                        if (currentUser != null)
                        {
                            // Log the action
                            _context.Logs.Add(new Log
                            {
                                UserId = currentUser.UserId,   // Correct User ID
                                Action = $"Asset '{asset.AssetName}' updated.",
                                Timestamp = DateTime.UtcNow,
                                PerformedBy = currentUser.Username  // Correct username reference
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Asset updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AssetExists(asset.AssetId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["AssignedUserId"] = new SelectList(_context.Users, "UserId", "Username", asset.AssignedUserId);
            return View(asset);
        }

        // GET: Assets/Delete
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Asset ID not provided.";
                return NotFound();
            }

            var asset = await _context.Assets
                .Include(a => a.AssignedUser)
                .FirstOrDefaultAsync(m => m.AssetId == id);

            if (asset == null)
            {
                TempData["Error"] = "Asset not found.";
                return NotFound();
            }

            return View(asset);
        }

        // POST: Assets/Delete
        [HttpPost, ActionName("Delete")]
        
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                TempData["Error"] = "Asset not found.";
                return RedirectToAction(nameof(Index));
            }
            
            // Find the current user
            var username = User?.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                TempData["Error"] = "User not recognized.";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (currentUser == null)
            {
                TempData["Error"] = "User not recognized.";
                return RedirectToAction(nameof(Index));
            }

            // Delete the asset
            _context.Assets.Remove(asset);

            // Log the deletion action
            _context.Logs.Add(new Log
            {
                UserId = currentUser.UserId,   // Correct User ID
                Action = $"Asset '{asset.AssetName}' deleted.",
                Timestamp = DateTime.UtcNow,
                PerformedBy = currentUser.Username  // Correct username reference
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Asset deleted successfully.";
            return RedirectToAction(nameof(Index));
        }


        private bool AssetExists(int id)
        {
            return _context.Assets.Any(e => e.AssetId == id);
        }
    }
}
