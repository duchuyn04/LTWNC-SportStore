using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsStore.Models;
using SportsStore.Models.ViewModels;

namespace SportsStore.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private StoreDbContext context;
        public ProductController(StoreDbContext ctx)
        {
            context = ctx;
        }

        [HttpGet]
        public IActionResult Product()
        {
            var list = GetCategories();
            TempData["CategoryList"] = JsonSerializer.Serialize(list);
            return View();
        }

        [HttpPost]
        public IActionResult Product(Product product, string? newCategory)
        {
            if (!string.IsNullOrEmpty(newCategory))
            {
                product.Category = newCategory;
            }
            if (ModelState.IsValid)
            {
                context.Products.Add(product);
                context.SaveChanges();
                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("ProductList");
            }
            var list = GetCategories();
            TempData["CategoryList"] = JsonSerializer.Serialize(list);
            return View(product);
        }

        [HttpGet]
        public IActionResult ProductList(int page = 1)
        {
            var products = GetProducts(page);
            var totalItems = GetCount();
            var paging = new PagingInfo
            {
                TotalItems = totalItems,
                ItemsPerPage = Setting.PageSize,
                CurrentPage = page
            };
            ViewBag.PagingInfo = paging;
            return View(products);
        }

        private List<Category> GetCategories()
        {
            var names = context.Products
                .Select(p => p.Category)
                .Distinct()
                .ToList();
            return names.Select((name, index) => new Category
            {
                Idcate = index + 1,
                NameCate = name
            }).ToList();
        }

        private List<Product> GetProducts(int page)
        {
            return context.Products
                .OrderByDescending(p => p.ProductID)
                .Skip((page - 1) * Setting.PageSize)
                .Take(Setting.PageSize)
                .ToList();
        }

        private int GetCount()
        {
            return context.Products.Count();
        }
    }
}
