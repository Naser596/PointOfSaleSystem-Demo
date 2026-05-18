using System.ComponentModel.DataAnnotations;
using WebApplication3.Controllers;

namespace WebApplication3.Models
{
    public class CardTerminalViewModel
    {
        // Hidden field to pass items back
        public string CartJson { get; set; } = "[]";

        public decimal TotalAmount { get; set; }

        public int ItemCount { get; set; }

        public string? DiscountCode { get; set; }

        [Required(ErrorMessage = "Card Number is required")]
        [Display(Name = "Card Number")]
        [StringLength(19, MinimumLength = 13, ErrorMessage = "Invalid Card Number")]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Expiry Date")]
        [RegularExpression(@"(0[1-9]|1[0-2])\/?([0-9]{2})", ErrorMessage = "Invalid Expiry (MM/YY)")]
        public string Expiry { get; set; } = string.Empty;

        [Required]
        [StringLength(4, MinimumLength = 3)]
        [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "Invalid CVV")]
        public string CVV { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Cardholder Name")]
        public string CardHolder { get; set; } = string.Empty;
    }
}
