namespace SportsStore.Models
{
    public class Cart
    {
        public List<CartLine> Lines { get; set; } = new List<CartLine>();

        public decimal ComputeTotalValue() =>
            Lines.Sum(l => l.Quantity * (l.Product?.Price ?? 0));
    }
}
