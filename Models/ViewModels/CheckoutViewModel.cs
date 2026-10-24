using System.Collections.Generic;

namespace FoodShoppingApp.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; }

        public decimal SubTotal { get; set; }

        public string DiscountCode { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal FinalTotal { get; set; }

        // Payment
        public string PaymentMethod { get; set; } // "COD"
    }
}
