using SportsStore.Services;
using SportsStore.Data;
using Microsoft.AspNetCore.Mvc;
using SportsStore.Models;

namespace SportsStore.ViewComponents
{
    public class CartSummaryViewComponent : ViewComponent
    {
        private readonly ICartService _cartService;

        public CartSummaryViewComponent(ICartService cartService)
        {
            _cartService = cartService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cart = await _cartService.GetCartAsync();
            var totalItems = cart.Lines.Sum(l => l.Quantity);
            return View(totalItems);
        }
    }
}
