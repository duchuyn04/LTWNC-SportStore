namespace SportsStore.Models
{
    public class CartLine
    {
        public long ProductID { get; set; }
        public int Quantity { get; set; }
        public Product? Product { get; set; }
    }
}
