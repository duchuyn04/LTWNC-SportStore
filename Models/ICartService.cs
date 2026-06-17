using SportsStore.Models.ViewModels;

namespace SportsStore.Models
{
    public interface ICartService
    {
        Task AddToCart(long productId, int quantity);
        Task RemoveLine(long productId);
        Task IncreaseQuantity(long productId);
        Task DecreaseQuantity(long productId);
        Task<Cart> GetCartAsync();
        Task ClearAsync();
        Task MergeSessionToDbAsync();
        Task CreateOrderAsync(CheckoutViewModel model);
    }
}
