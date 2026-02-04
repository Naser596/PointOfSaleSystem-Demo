using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    /// <summary>
    /// Represents a single item in the shopping cart during checkout.
    /// </summary>
    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total => Price * Quantity;
    }

    /// <summary>
    /// ViewModel for the checkout/payment flow containing cart data and payment info.
    /// </summary>
    public class CheckoutViewModel
    {
        // Cart Data
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();
        public decimal TotalAmount { get; set; }
        
        // JSON representation of cart for form posting
        public string? CartJson { get; set; }

        // Payment Method
        [Required(ErrorMessage = "Please select a payment method")]
        public string PaymentMethod { get; set; } = "Cash";

        // Card Fields (required only for DebitCard)
        public string? CardholderName { get; set; }
        public string? CardNumber { get; set; }
        public string? CardExpiry { get; set; }
        public string? CardCVV { get; set; }

        // Billing Address
        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = string.Empty;
        
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
    }

    /// <summary>
    /// ViewModel for the payment success page.
    /// </summary>
    public class PaymentSuccessViewModel
    {
        public string SaleNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
    }
}
