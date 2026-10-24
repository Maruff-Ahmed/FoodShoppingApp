using System.ComponentModel.DataAnnotations;

namespace FoodShoppingApp.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }


        [Required]
        public int? CategoryId { get; set; }


        public Category Category { get; set; } 

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; }
        public string Description { get; set; }
    }
}
