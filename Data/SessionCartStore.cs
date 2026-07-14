using SportsStore.Models;
using System.Text.Json;

namespace SportsStore.Data
{
    public class SessionCartStore : ICartStore
    {
        private readonly IHttpContextAccessor _http;
        private const string CartKey = "Cart";

        public SessionCartStore(IHttpContextAccessor http)
        {
            _http = http;
        }

        private ISession Session => _http.HttpContext!.Session;

        public Task<Cart> GetAsync()
        {
            var data = Session.GetString(CartKey);
            var cart = data == null
                ? new Cart()
                : JsonSerializer.Deserialize<Cart>(data) ?? new Cart();
            return Task.FromResult(cart);
        }

        public Task SaveAsync(Cart cart)
        {
            Session.SetString(CartKey, JsonSerializer.Serialize(cart));
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            Session.Remove(CartKey);
            return Task.CompletedTask;
        }
    }
}
