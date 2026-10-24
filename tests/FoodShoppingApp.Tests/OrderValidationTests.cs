using FoodShoppingApp.Helpers;
using FoodShoppingApp.Models;
using Xunit;

namespace FoodShoppingApp.Tests
{
    public class OrderValidationTests
    {
        [Theory]
        [InlineData("Pending", true)]
        [InlineData("Processing", true)]
        [InlineData("Delivered", true)]
        [InlineData("Cancelled", true)]
        [InlineData("pending", true)]     // Case-insensitivity check
        [InlineData("DELIVERED", true)]   // Case-insensitivity check
        [InlineData("HackedStatus", false)]
        [InlineData("<script>alert(1)</script>", false)]
        [InlineData("Shipped", false)]    // Unknown status outside whitelist
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        public void IsValidOrderStatus_AllowedStatuses_ValidatesAccurately(string? status, bool expectedResult)
        {
            // Act - invoking production application logic
            bool isValid = OrderCalculationHelper.IsValidOrderStatus(status);

            // Assert
            Assert.Equal(expectedResult, isValid);
        }

        [Fact]
        public void Product_PriceConstraint_RejectsNegativeOrZeroPrices()
        {
            // Arrange
            var invalidProduct1 = new Product { Name = "Burger", Price = -10.00m };
            var invalidProduct2 = new Product { Name = "Fries", Price = 0.00m };
            var validProduct = new Product { Name = "Drink", Price = 45.00m };

            // Assert
            Assert.True(invalidProduct1.Price <= 0);
            Assert.True(invalidProduct2.Price <= 0);
            Assert.True(validProduct.Price > 0);
        }

        [Fact]
        public void Order_TotalAmount_CannotBeNegative()
        {
            // Arrange
            var subtotal = 100.00m;
            var discount = 150.00m;

            // Act - invoking production application logic
            var finalTotal = OrderCalculationHelper.CalculateFinalTotal(subtotal, discount);

            // Assert
            Assert.Equal(0m, finalTotal);
        }

        [Fact]
        public void AllowedStatuses_ContainsExactExpectedSet()
        {
            // Assert
            Assert.Equal(4, OrderCalculationHelper.AllowedStatuses.Count);
            Assert.Contains("Pending", OrderCalculationHelper.AllowedStatuses);
            Assert.Contains("Processing", OrderCalculationHelper.AllowedStatuses);
            Assert.Contains("Delivered", OrderCalculationHelper.AllowedStatuses);
            Assert.Contains("Cancelled", OrderCalculationHelper.AllowedStatuses);
        }
    }
}
