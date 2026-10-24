using System.ComponentModel.DataAnnotations;

namespace FoodShoppingApp.Models
{
    public class Discount
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Promo code is required.")]
        public string Code { get; set; } = string.Empty;

        [Range(1, 100, ErrorMessage = "Percentage must be between 1% and 100%.")]
        public int Percentage { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
