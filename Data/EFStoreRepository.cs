using SportsStore.Models;
using Microsoft.EntityFrameworkCore;

namespace SportsStore.Data
{
    public class EFStoreRepository : IStoreRepository
    {
        private StoreDbContext context;
        public EFStoreRepository(StoreDbContext ctx)
        {
            context = ctx;
        }
        public IQueryable<Product> Products => context.Products.Include(p => p.Images);
    }
}
