namespace SportsStore.Models
{
    public class Order
    {
        public long OrderID { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime OrderedAt { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public List<OrderLine> Lines { get; set; } = new List<OrderLine>();
    }
}
