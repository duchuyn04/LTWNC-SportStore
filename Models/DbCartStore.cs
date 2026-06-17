using Microsoft.EntityFrameworkCore;

namespace SportsStore.Models
{
    public class DbCartStore : ICartStore
    {
        private readonly StoreDbContext _context;
        private readonly string _userId;

        public DbCartStore(StoreDbContext context, string userId)
        {
            _context = context;
            _userId = userId;
        }

        public async Task<Cart> GetAsync()
        {
            var items = await _context.CartItems
                .Where(c => c.UserId == _userId)
                .ToListAsync();

            var cart = new Cart();
            foreach (var item in items)
            {
                cart.Lines.Add(new CartLine
                {
                    ProductID = item.ProductId ?? 0,
                    Quantity = item.Quantity
                });
            }
            return cart;
        }

        public async Task SaveAsync(Cart cart)
        {
            var existing = await _context.CartItems
                .Where(c => c.UserId == _userId)
                .ToListAsync();

            _context.CartItems.RemoveRange(existing);

            foreach (var line in cart.Lines)
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = _userId,
                    ProductId = line.ProductID,
                    Quantity = line.Quantity
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task ClearAsync()
        {
            var existing = await _context.CartItems
                .Where(c => c.UserId == _userId)
                .ToListAsync();

            _context.CartItems.RemoveRange(existing);
            await _context.SaveChangesAsync();
        }
    }
}
