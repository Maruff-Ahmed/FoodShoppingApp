using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoodShoppingApp.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public IdentityUser? User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public decimal TotalAmount { get; set; }

        public string PaymentMethod { get; set; } = "Cash On Delivery";

        public string DiscountCode { get; set; } = string.Empty;

        public decimal DiscountAmount { get; set; }

        public string Status { get; set; } = "Pending";

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
