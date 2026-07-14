using Microsoft.AspNetCore.Mvc;
using SportsStore.Models;

namespace SportsStore.ViewComponents
{
    public class ProductDescriptionTabsViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Product product)
        {
            return View(product);
        }
    }
}
