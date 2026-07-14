using SportsStore.Models;

namespace SportsStore.Models.ViewModels
{
    public class ProductReviewsViewModel
    {
        public long ProductId { get; set; }
        public List<ProductReview> Reviews { get; set; } = new List<ProductReview>();
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsAuthenticated { get; set; }
        public string? CurrentUserName { get; set; }
    }
}
