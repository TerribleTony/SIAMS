using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAMS.Data;
using System.Threading.Tasks;

[Authorize(Roles = "Admin")]  // Restrict access to admins
public class LogsController : Controller
{
    private readonly ApplicationDbContext _context;

    public LogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Logs
    public async Task<IActionResult> Index()
    {
        var logs = await _context.Logs
            .Include(l => l.User)  // Correctly include User as a navigation property
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();

        return View(logs);
    }
}
