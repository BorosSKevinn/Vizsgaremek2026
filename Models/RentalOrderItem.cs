namespace VizsgaremekDemo.Models
{
    public class RentalOrderItem
    {
        public int Id { get; set; }
        public int RentalOrderId { get; set; }
        public int RentalItemId { get; set; }
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public DateTime RentalStartDate { get; set; }
        public DateTime RentalEndDate { get; set; }
    }
}
