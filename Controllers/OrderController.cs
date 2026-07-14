using SportsStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsStore.Models;

namespace SportsStore.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly StoreDbContext _context;

        public OrderController(StoreDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> List()
        {
            var userId = User.Identity?.Name ?? string.Empty;
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
                .OrderByDescending(o => o.OrderedAt)
                .ToListAsync();

            return View(orders);
        }
    }
}
