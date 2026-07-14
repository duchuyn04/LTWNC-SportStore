using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsStore.Models;

namespace SportsStore.ViewComponents
{
    public class ProductGalleryViewComponent : ViewComponent
    {
        private readonly StoreDbContext _context;

        public ProductGalleryViewComponent(StoreDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(long productId)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            return View(product);
        }
    }
}
