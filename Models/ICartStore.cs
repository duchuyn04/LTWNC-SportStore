namespace SportsStore.Models
{
    public interface ICartStore
    {
        Task<Cart> GetAsync();
        Task SaveAsync(Cart cart);
        Task ClearAsync();
    }
}
