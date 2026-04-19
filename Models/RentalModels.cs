using System.ComponentModel.DataAnnotations;

namespace Vizsgaremek2026.Models
{
    public class RentalItem
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;
        
        public int Capacity { get; set; }
        
        public decimal PricePerSession { get; set; }
        
        public double Rating { get; set; }
        
        [StringLength(1000)]
        public string[] Tags { get; set; } = [];

        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual ICollection<RentalOrder> RentalOrders { get; set; } = new List<RentalOrder>();
        public virtual ICollection<RentalAvailability> RentalAvailabilities { get; set; } = new List<RentalAvailability>();
        public List<(DateTime From, DateTime To)> Busy { get; set; } = new();
    }

    public class RentalOrder
    {
        public int Id { get; set; }
        
        public int RentalItemId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string CustomerEmail { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string CustomerAddress { get; set; } = string.Empty;
        [StringLength(100)]
        public string CustomerCity { get; set; } = string.Empty;
        [StringLength(20)]
        public string CustomerPostalCode { get; set; } = string.Empty;
        [StringLength(100)]
        public string VatNumber { get; set; } = string.Empty;

        [Required]
        public DateTime RentalStartDate { get; set; }
        
        [Required]
        public DateTime RentalEndDate { get; set; }
        
        public int Quantity { get; set; } = 1;
        
        public decimal PricePerSession { get; set; }
        
        public decimal TotalAmount { get; set; }
        
        [StringLength(20)]
        public string PaymentMethod { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? StripeSessionId { get; set; }
        
        [StringLength(100)]
        public string? StripePaymentIntentId { get; set; }
        
        [StringLength(100)]
        public string? StripeChargeId { get; set; }
        
        [StringLength(1000)]
        public string? PaymentProviderResponse { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "pending";
        
        [StringLength(20)]
        public string PaymentStatus { get; set; } = "pending";
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
        public DateTime? PaymentCompletedAt { get; set; }
        
        public virtual RentalItem RentalItem { get; set; } = null!;

        public List<RentalOrderItem> Items { get; set; } = new();

    }

    public class RentalAvailability
    {
        public int Id { get; set; }
        
        public int RentalItemId { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [StringLength(20)]
        public string Status { get; set; } = "unavailable";
        
        [StringLength(500)]
        public string? Reason { get; set; }
        
        public int? RelatedOrderId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual RentalItem RentalItem { get; set; } = null!;
        
        public virtual RentalOrder? RelatedOrder { get; set; }
    }


}