using SportsStore.Models;
namespace SportsStore.Data
{
    public interface ICartStore
    {
        Task<Cart> GetAsync();
        Task SaveAsync(Cart cart);
        Task ClearAsync();
    }
}
