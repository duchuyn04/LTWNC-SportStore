using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsStore.Models;
using SportsStore.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SportsStore.Controllers
{
    public class HomeController : Controller
    {
        private IStoreRepository repository;
        private readonly StoreDbContext _context;
        public int PageSize = 4;
        public int LandingFeaturedCount = 4;

        public HomeController(IStoreRepository repo, StoreDbContext context)
        {
            repository = repo;
            _context = context;
        }

        public ViewResult Index()
        {
            var categories = repository.Products
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList()
                .Select((name, index) => new Category { Idcate = index + 1, NameCate = name })
                .ToList();

            var categoryCounts = repository.Products
                .AsEnumerable()
                .GroupBy(p => p.Category)
                .ToDictionary(g => g.Key ?? "Khác", g => g.Count());

            var featured = repository.Products
                .OrderBy(p => p.ProductID)
                .Take(LandingFeaturedCount)
                .ToList();

            return View(new LandingViewModel
            {
                Categories = categories,
                CategoryCounts = categoryCounts,
                FeaturedProducts = featured
            });
        }

        public ViewResult Products(int productPage = 1)
            => View(new ProductsListViewModel
            {
                Products = repository.Products
                    .OrderBy(p => p.ProductID)
                    .Skip((productPage - 1) * PageSize)
                    .Take(PageSize),
                PagingInfo = new PagingInfo
                {
                    CurrentPage = productPage,
                    ItemsPerPage = PageSize,
                    TotalItems = repository.Products.Count()
                }
            });

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddReview(ProductReview review, long productId)
        {
            review.ProductID = productId;
            review.UserName = User.Identity?.Name ?? "Anonymous";
            review.CreatedAt = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin đánh giá.";
                return RedirectToAction("Detail", new { id = productId });
            }

            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi đánh giá!";
            return RedirectToAction("Detail", new { id = productId });
        }
    }
}
