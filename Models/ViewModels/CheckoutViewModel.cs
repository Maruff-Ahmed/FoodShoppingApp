using System.Collections.Generic;

namespace FoodShoppingApp.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; } = new();

        public decimal SubTotal { get; set; }

        public string? DiscountCode { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal FinalTotal { get; set; }

        public string PaymentMethod { get; set; } = "Cash On Delivery";

        public string? CheckoutToken { get; set; }
    }
}
