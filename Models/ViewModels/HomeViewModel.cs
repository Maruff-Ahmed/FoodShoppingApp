using FoodShoppingApp.Models;

namespace FoodShoppingApp.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; } = new();
        public List<Product> FeaturedProducts { get; set; } = new();
        public List<Product> DealOfTheDayProducts { get; set; } = new();
        public List<Product> TrendingProducts { get; set; } = new();
    }
}
