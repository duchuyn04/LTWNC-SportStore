using System.ComponentModel.DataAnnotations.Schema;

namespace SportsStore.Models
{
    public class OrderLine
    {
        public long OrderLineID { get; set; }
        public long OrderID { get; set; }
        public long? ProductID { get; set; }
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(8, 2)")]
        public decimal Price { get; set; }

        public Product? Product { get; set; }
    }
}
