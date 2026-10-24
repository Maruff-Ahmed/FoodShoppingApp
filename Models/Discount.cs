using System.ComponentModel.DataAnnotations;

namespace FoodShoppingApp.Models
{
    public class Discount
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; }   // e.g. SAVE10

        [Range(1, 100)]
        public int Percentage { get; set; } // 10 = 10%

        public bool IsActive { get; set; }
    }
}
