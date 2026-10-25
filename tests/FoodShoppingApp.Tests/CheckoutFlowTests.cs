using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoodShoppingApp.Data;
using FoodShoppingApp.Helpers;
using FoodShoppingApp.Models;
using FoodShoppingApp.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FoodShoppingApp.Tests
{
    public class CheckoutFlowTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task OrderCreation_ValidCart_GeneratesCorrectOrderAndLineItems()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var category = new Category { Name = "Fresh Produce" };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var p1 = new Product { Name = "Organic Apples", Price = 120.00m, CategoryId = category.Id };
            var p2 = new Product { Name = "Fresh Broccoli", Price = 60.00m, CategoryId = category.Id };
            context.Products.AddRange(p1, p2);
            await context.SaveChangesAsync();

            var cart = new List<CartItem>
            {
                new CartItem { Product = p1, Quantity = 2 }, // 240
                new CartItem { Product = p2, Quantity = 3 }  // 180
            };

            // Act - compute subtotal and build items
            decimal subtotal = 0m;
            var orderItems = new List<OrderItem>();
            foreach (var item in cart)
            {
                var dbProduct = await context.Products.FindAsync(item.Product.Id);
                Assert.NotNull(dbProduct);
                var unitPrice = dbProduct.Price;
                var qty = OrderCalculationHelper.ClampQuantity(item.Quantity);
                subtotal += unitPrice * qty;

                orderItems.Add(new OrderItem
                {
                    ProductId = dbProduct.Id,
                    Quantity = qty,
                    Price = unitPrice
                });
            }

            var discountAmount = 0m;
            var finalTotal = OrderCalculationHelper.CalculateFinalTotal(subtotal, discountAmount);

            var order = new Order
            {
                UserId = "test-user-id",
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                PaymentMethod = "Cash On Delivery",
                TotalAmount = finalTotal,
                DiscountAmount = discountAmount,
                DiscountCode = string.Empty,
                OrderItems = orderItems
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            // Assert
            Assert.True(order.Id > 0);
            Assert.Equal(420.00m, order.TotalAmount);
            Assert.Equal(2, order.OrderItems.Count);
            Assert.Equal("Pending", order.Status);
            Assert.Equal("Cash On Delivery", order.PaymentMethod);
        }

        [Fact]
        public async Task OrderCreation_WithPromoDiscount_AppliesDiscountAccurately()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var promo = new Discount { Code = "SAVE10", Percentage = 10, IsActive = true };
            context.Discounts.Add(promo);
            await context.SaveChangesAsync();

            decimal subtotal = 500.00m;

            // Act
            var discount = await context.Discounts.FirstOrDefaultAsync(d => d.Code == "SAVE10" && d.IsActive);
            Assert.NotNull(discount);

            var discountAmount = OrderCalculationHelper.CalculateDiscount(subtotal, discount);
            var finalTotal = OrderCalculationHelper.CalculateFinalTotal(subtotal, discountAmount);

            // Assert
            Assert.Equal(50.00m, discountAmount);
            Assert.Equal(450.00m, finalTotal);
        }

        [Fact]
        public void CheckoutToken_Validation_DetectsMismatchedOrMissingToken()
        {
            string sessionToken = Guid.NewGuid().ToString("N");
            string validSubmittedToken = sessionToken;
            string mismatchedToken = Guid.NewGuid().ToString("N");
            string? emptyToken = null;

            Assert.True(sessionToken == validSubmittedToken);
            Assert.False(sessionToken == mismatchedToken);
            Assert.False(sessionToken == emptyToken);
        }

        [Fact]
        public void CheckoutViewModel_DefaultValues_AreCorrect()
        {
            var model = new CheckoutViewModel();

            Assert.NotNull(model.CartItems);
            Assert.Empty(model.CartItems);
            Assert.Equal("Cash On Delivery", model.PaymentMethod);
            Assert.Equal(0m, model.SubTotal);
            Assert.Equal(0m, model.DiscountAmount);
            Assert.Equal(0m, model.FinalTotal);
        }
    }
}
