using SportsStore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsStore.Models;
using SportsStore.Models.ViewModels;

namespace SportsStore.ViewComponents
{
    public class ProductReviewsViewComponent : ViewComponent
    {
        private readonly StoreDbContext _context;

        public ProductReviewsViewComponent(StoreDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(long productId)
        {
            var reviews = await _context.ProductReviews
                .Where(r => r.ProductID == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var avg = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            return View(new ProductReviewsViewModel
            {
                ProductId = productId,
                Reviews = reviews,
                AverageRating = avg,
                ReviewCount = reviews.Count,
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                CurrentUserName = User.Identity?.Name
            });
        }
    }
}
