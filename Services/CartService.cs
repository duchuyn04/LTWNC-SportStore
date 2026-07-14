using Microsoft.EntityFrameworkCore;
using SportsStore.Data;
using SportsStore.Models;
using SportsStore.Models.ViewModels;

namespace SportsStore.Services
{
    public class CartService : ICartService
    {
        private readonly ICartStore _store;
        private readonly IStoreRepository _repo;
        private readonly StoreDbContext _context;
        private readonly IHttpContextAccessor _http;

        public CartService(ICartStore store, IStoreRepository repo,
            StoreDbContext context, IHttpContextAccessor http)
        {
            _store = store;
            _repo = repo;
            _context = context;
            _http = http;
        }

        public async Task AddToCart(long productId, int quantity)
        {
            var exists = await _repo.Products.AnyAsync(p => p.ProductID == productId);
            if (!exists) return;

            if (quantity < 1) quantity = 1;

            var cart = await _store.GetAsync();
            var line = cart.Lines.FirstOrDefault(l => l.ProductID == productId);
            if (line != null)
                line.Quantity += quantity;
            else
                cart.Lines.Add(new CartLine { ProductID = productId, Quantity = quantity });

            await _store.SaveAsync(cart);
        }

        public async Task RemoveLine(long productId)
        {
            var cart = await _store.GetAsync();
            cart.Lines.RemoveAll(l => l.ProductID == productId);
            await _store.SaveAsync(cart);
        }

        public async Task IncreaseQuantity(long productId)
        {
            var cart = await _store.GetAsync();
            var line = cart.Lines.FirstOrDefault(l => l.ProductID == productId);
            if (line != null)
            {
                line.Quantity++;
                await _store.SaveAsync(cart);
            }
        }

        public async Task DecreaseQuantity(long productId)
        {
            var cart = await _store.GetAsync();
            var line = cart.Lines.FirstOrDefault(l => l.ProductID == productId);
            if (line != null)
            {
                line.Quantity--;
                if (line.Quantity <= 0)
                    cart.Lines.Remove(line);
                await _store.SaveAsync(cart);
            }
        }

        public async Task<Cart> GetCartAsync()
        {
            var cart = await _store.GetAsync();
            foreach (var line in cart.Lines)
            {
                line.Product = await _repo.Products
                    .FirstOrDefaultAsync(p => p.ProductID == line.ProductID);
            }
            return cart;
        }

        public async Task ClearAsync() => await _store.ClearAsync();

        public async Task MergeSessionToDbAsync()
        {
            var httpContext = _http.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true) return;
            await MergeSessionToDbAsync(httpContext.User.Identity.Name!);
        }

        public async Task MergeSessionToDbAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            var sessionStore = new SessionCartStore(_http);
            var sessionCart = await sessionStore.GetAsync();
            if (!sessionCart.Lines.Any()) return;

            var dbStore = new DbCartStore(_context, userId);
            var dbCart = await dbStore.GetAsync();

            foreach (var line in sessionCart.Lines)
            {
                var existing = dbCart.Lines.FirstOrDefault(l => l.ProductID == line.ProductID);
                if (existing != null)
                    existing.Quantity += line.Quantity;
                else
                    dbCart.Lines.Add(new CartLine
                    {
                        ProductID = line.ProductID,
                        Quantity = line.Quantity
                    });
            }

            await dbStore.SaveAsync(dbCart);
            await sessionStore.ClearAsync();
        }

        public async Task CreateOrderAsync(CheckoutViewModel model)
        {
            var httpContext = _http.HttpContext;
            var userId = httpContext?.User?.Identity?.Name
                ?? throw new InvalidOperationException("User must be authenticated to create an order.");

            var cart = await GetCartAsync();
            if (!cart.Lines.Any()) return;

            var order = new Order
            {
                UserId = userId,
                OrderedAt = DateTime.UtcNow,
                Name = model.Name,
                Address = model.Address,
                Phone = model.Phone,
                TotalValue = cart.ComputeTotalValue(),
                Lines = cart.Lines.Select(l => new OrderLine
                {
                    ProductID = l.ProductID,
                    Quantity = l.Quantity,
                    Price = l.Product?.Price ?? 0
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
        }
    }
}
