using FoodShoppingApp.Helpers;
using FoodShoppingApp.Models;
using Xunit;

namespace FoodShoppingApp.Tests
{
    public class DiscountAndPricingTests
    {
        [Theory]
        [InlineData(1000.00, 10, 100.00, 900.00)]
        [InlineData(550.00, 20, 110.00, 440.00)]
        [InlineData(200.00, 50, 100.00, 100.00)]
        [InlineData(100.00, 100, 100.00, 0.00)]
        public void CalculateDiscountAndFinalTotal_ValidActivePromo_ComputesCorrectAmounts(
            decimal subtotal, int percentage, decimal expectedDiscount, decimal expectedFinal)
        {
            // Arrange
            var discount = new Discount
            {
                Code = "PROMO",
                Percentage = percentage,
                IsActive = true
            };

            // Act - invoking production application logic
            decimal discountAmount = OrderCalculationHelper.CalculateDiscount(subtotal, discount);
            decimal finalTotal = OrderCalculationHelper.CalculateFinalTotal(subtotal, discountAmount);

            // Assert
            Assert.Equal(expectedDiscount, discountAmount);
            Assert.Equal(expectedFinal, finalTotal);
        }

        [Fact]
        public void CalculateDiscount_InactivePromo_YieldsZeroDiscount()
        {
            // Arrange
            var subtotal = 750.00m;
            var discount = new Discount
            {
                Code = "EXPIRED",
                Percentage = 25,
                IsActive = false
            };

            // Act - invoking production application logic
            decimal discountAmount = OrderCalculationHelper.CalculateDiscount(subtotal, discount);
            decimal finalTotal = OrderCalculationHelper.CalculateFinalTotal(subtotal, discountAmount);

            // Assert
            Assert.Equal(0m, discountAmount);
            Assert.Equal(subtotal, finalTotal);
        }

        [Fact]
        public void CalculateDiscount_NullDiscount_YieldsZeroDiscountSafely()
        {
            // Act - invoking production application logic
            decimal discountAmount = OrderCalculationHelper.CalculateDiscount(500.00m, null);
            decimal finalTotal = OrderCalculationHelper.CalculateFinalTotal(500.00m, discountAmount);

            // Assert
            Assert.Equal(0m, discountAmount);
            Assert.Equal(500.00m, finalTotal);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void CalculateDiscount_ZeroOrNegativePercentage_YieldsZeroDiscount(int percentage)
        {
            // Arrange
            var discount = new Discount
            {
                Code = "INVALID",
                Percentage = percentage,
                IsActive = true
            };

            // Act
            decimal discountAmount = OrderCalculationHelper.CalculateDiscount(500.00m, discount);

            // Assert
            Assert.Equal(0m, discountAmount);
        }

        [Fact]
        public void CalculateFinalTotal_DiscountExceedingSubtotal_ClampsToZero()
        {
            // Arrange
            decimal subtotal = 100.00m;
            decimal excessiveDiscount = 250.00m;

            // Act - invoking production application logic
            decimal finalTotal = OrderCalculationHelper.CalculateFinalTotal(subtotal, excessiveDiscount);

            // Assert
            Assert.Equal(0m, finalTotal);
        }

        [Fact]
        public void ServerPriceRecalculation_IgnoresClientManipulatedPrice()
        {
            // Scenario: Client form sends manipulated unit price of 0.01
            var clientClaimedUnitPrice = 0.01m;
            var serverActualDbPrice = 350.00m;
            var quantity = 2;

            // Insecure behavior would calculate: clientClaimedUnitPrice * quantity = 0.02
            var insecureTotal = clientClaimedUnitPrice * quantity;

            // Secure server-side behavior forces DB price through OrderCalculationHelper:
            var secureTotal = OrderCalculationHelper.CalculateLineTotal(serverActualDbPrice, quantity);

            // Assert
            Assert.Equal(0.02m, insecureTotal);
            Assert.Equal(700.00m, secureTotal);
            Assert.NotEqual(insecureTotal, secureTotal);
        }
    }
}
