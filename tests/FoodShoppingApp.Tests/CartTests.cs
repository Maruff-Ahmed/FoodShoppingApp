using FoodShoppingApp.Helpers;
using FoodShoppingApp.Models;
using Xunit;

namespace FoodShoppingApp.Tests
{
    public class CartTests
    {
        [Fact]
        public void CalculateLineTotal_ValidPriceAndQuantity_CalculatesAccurately()
        {
            // Arrange
            decimal price = 150.50m;
            int quantity = 3;

            // Act - invoking production application logic
            decimal total = OrderCalculationHelper.CalculateLineTotal(price, quantity);

            // Assert
            Assert.Equal(451.50m, total);
        }

        [Fact]
        public void CalculateSubtotal_MultipleItems_CalculatesGrandTotalCorrectly()
        {
            // Arrange
            var cart = new List<CartItem>
            {
                new CartItem { Product = new Product { Id = 1, Price = 120.00m }, Quantity = 2 }, // 240
                new CartItem { Product = new Product { Id = 2, Price = 50.00m }, Quantity = 3 },  // 150
                new CartItem { Product = new Product { Id = 3, Price = 25.50m }, Quantity = 4 }   // 102
            };

            // Act - invoking production application logic
            decimal subtotal = OrderCalculationHelper.CalculateSubtotal(cart);

            // Assert
            Assert.Equal(492.00m, subtotal);
        }

        [Fact]
        public void CalculateSubtotal_EmptyCart_ReturnsZero()
        {
            // Arrange
            var emptyCart = new List<CartItem>();

            // Act
            decimal subtotal = OrderCalculationHelper.CalculateSubtotal(emptyCart);

            // Assert
            Assert.Equal(0m, subtotal);
        }

        [Fact]
        public void CalculateSubtotal_NullCollection_ReturnsZeroSafely()
        {
            // Act
            decimal subtotal = OrderCalculationHelper.CalculateSubtotal(null);

            // Assert
            Assert.Equal(0m, subtotal);
        }

        [Theory]
        [InlineData(-5, 1)]      // Negative clamped to minimum 1
        [InlineData(0, 1)]       // Zero clamped to minimum 1
        [InlineData(1, 1)]       // Minimum boundary
        [InlineData(25, 25)]     // In-range value
        [InlineData(50, 50)]     // Maximum boundary
        [InlineData(51, 50)]     // Exceeding clamped to maximum 50
        [InlineData(999, 50)]    // Large quantity clamped to maximum 50
        public void ClampQuantity_EnforcesAllowedRange_Between1And50(int inputQuantity, int expectedQuantity)
        {
            // Act - invoking production application logic
            int clamped = OrderCalculationHelper.ClampQuantity(inputQuantity);

            // Assert
            Assert.Equal(expectedQuantity, clamped);
        }
    }
}
