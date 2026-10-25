namespace FoodShoppingApp.Models
{
    public class CartItem
    {
        public Product Product { get; set; } = new Product();
        public int Quantity { get; set; } = 1;
    }

    /// <summary>
    /// Minimal DTO stored in user session to prevent EF Core navigation cycle serialization errors.
    /// Never stores complex navigation graphs or unverified prices.
    /// </summary>
    public class CartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal Price { get; set; }
    }
}
