using SportsStore.Services;
using SportsStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsStore.Models;
using SportsStore.Models.ViewModels;

namespace SportsStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _cartService.GetCartAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(long productId, int quantity, string? returnUrl)
        {
            await _cartService.AddToCart(productId, quantity);
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Remove(long productId)
        {
            await _cartService.RemoveLine(productId);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Plus(long productId)
        {
            await _cartService.IncreaseQuantity(productId);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Minus(long productId)
        {
            await _cartService.DecreaseQuantity(productId);
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cart = await _cartService.GetCartAsync();
            if (!cart.Lines.Any())
            {
                TempData["Message"] = "Giỏ hàng trống.";
                return RedirectToAction("Index");
            }
            return View(new CheckoutViewModel());
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var cart = await _cartService.GetCartAsync();
            if (!cart.Lines.Any())
            {
                TempData["Message"] = "Giỏ hàng trống.";
                return RedirectToAction("Index");
            }

            await _cartService.CreateOrderAsync(model);
            await _cartService.ClearAsync();
            return RedirectToAction("Completed");
        }

        public IActionResult Completed() => View();
    }
}
