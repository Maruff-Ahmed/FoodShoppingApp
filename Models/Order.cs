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
        public string UserId { get; set; }
        public IdentityUser User { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string DiscountCode { get; set; }
        public decimal DiscountAmount { get; set; }

        public string Status { get; set; } = "Pending";
        public ICollection<OrderItem> OrderItems { get; set; }
    }
}
