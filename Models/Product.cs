using System.ComponentModel.DataAnnotations;

namespace FoodShoppingApp.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required.")]
        public int? CategoryId { get; set; }

        public Category? Category { get; set; }

        [Required]
        [Range(0.01, 100000, ErrorMessage = "Price must be between ৳ 0.01 and ৳ 100,000.")]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}
