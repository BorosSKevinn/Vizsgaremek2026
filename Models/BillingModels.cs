using System.ComponentModel.DataAnnotations;

namespace Vizsgaremek2026.Models
{
    public class BillingDetailsModel
    {
        [Required]
        [Display(Name = "Teljes név")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "E-mail cím")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [Display(Name = "Telefonszám")]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Irányítószám")]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Város")]
        public string City { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Cím")]
        public string Address { get; set; } = string.Empty;
        [Display(Name = "Adószám")]
        public string VatNumber { get; set; } = "";

        [Display(Name = "További címadat (emelet, ajtó, stb.)")]
        public string AddressLine2 { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Fizetési mód")]
        public PaymentMethod PaymentMethod { get; set; }

        public List<OrderItem> Items { get; set; } = new();
        public decimal Total { get; set; }
    }

    public class OrderItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price;
        
        public int RentalItemId { get; set; }
        public DateTime RentalStartDate { get; set; }
        public DateTime RentalEndDate { get; set; }
    }

    public enum PaymentMethod
    {
        [Display(Name = "Bankkártya")]
        Card = 1,
        
        [Display(Name = "Utánvét")]
        CashOnDelivery = 2
    }
    public class PaymentRequest
    {
        public BillingDetailsModel BillingDetails { get; set; } = new();
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;

        public int RentalItemId { get; set; }
        public DateTime RentalStartDate { get; set; }
        public DateTime RentalEndDate { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Notes { get; set; }
    }
    public class PaymentResponse
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string? SessionId { get; set; }
        public string? CheckoutUrl { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}