using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsStore.Models;

namespace SportsStore.ViewComponents
{
    public class RelatedProductsViewComponent : ViewComponent
    {
        private readonly StoreDbContext _context;

        public RelatedProductsViewComponent(StoreDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string category, long excludeId)
        {
            var products = await _context.Products
                .Include(p => p.Images)
                .Where(p => p.Category == category && p.ProductID != excludeId)
                .OrderBy(p => p.ProductID)
                .Take(4)
                .ToListAsync();

            return View(products);
        }
    }
}
