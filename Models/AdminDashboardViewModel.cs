using System.Collections.Generic;

namespace FoodShoppingApp.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int ActiveDiscounts { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
    }
}
