namespace SportsStore.Models
{
    public class CartItem
    {
        public int CartItemId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public long? ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
