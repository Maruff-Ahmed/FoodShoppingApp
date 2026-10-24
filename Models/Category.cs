using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace FoodShoppingApp.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public ICollection<Product> Products { get; set; }
    }
}
