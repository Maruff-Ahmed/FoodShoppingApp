using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FoodShoppingApp.Helpers;
using FoodShoppingApp.Models;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace FoodShoppingApp.Tests
{
    public class MockSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id => "mock-session-id";
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }

    public class CartSessionTests
    {
        [Fact]
        public void CartItemDto_SerializesWithoutCycleException()
        {
            // Arrange
            var items = new List<CartItemDto>
            {
                new CartItemDto { ProductId = 1, Quantity = 2, Price = 100m },
                new CartItemDto { ProductId = 2, Quantity = 5, Price = 50m }
            };

            // Act & Assert - Ensure standard JSON serializer can serialize without circular reference exception
            var json = JsonSerializer.Serialize(items);
            Assert.False(string.IsNullOrEmpty(json));
            Assert.Contains("\"ProductId\":1", json);
            Assert.Contains("\"ProductId\":2", json);
        }

        [Fact]
        public void SetCartItems_StoresMinimalRepresentation_And_GetCartItems_RetrievesAccurately()
        {
            // Arrange
            var session = new MockSession();
            var input = new List<CartItemDto>
            {
                new CartItemDto { ProductId = 10, Quantity = 3, Price = 45.00m },
                new CartItemDto { ProductId = 20, Quantity = 1, Price = 80.00m }
            };

            // Act
            session.SetCartItems(input);
            var retrieved = session.GetCartItems();

            // Assert
            Assert.Equal(2, retrieved.Count);
            Assert.Equal(10, retrieved[0].ProductId);
            Assert.Equal(3, retrieved[0].Quantity);
            Assert.Equal(45.00m, retrieved[0].Price);
            Assert.Equal(20, retrieved[1].ProductId);
            Assert.Equal(1, retrieved[1].Quantity);
            Assert.Equal(80.00m, retrieved[1].Price);
        }

        [Fact]
        public void GetCartItems_LegacySerializedProductFormat_ParsesCorrectly()
        {
            // Arrange - simulate old session JSON where full Product was serialized
            var session = new MockSession();
            var legacyJson = @"[
                {
                    ""Product"": {
                        ""Id"": 42,
                        ""Name"": ""Fresh Strawberry"",
                        ""Price"": 120.50
                    },
                    ""Quantity"": 4
                }
            ]";
            session.SetString("Cart", legacyJson);

            // Act
            var items = session.GetCartItems();

            // Assert
            Assert.Single(items);
            Assert.Equal(42, items[0].ProductId);
            Assert.Equal(4, items[0].Quantity);
            Assert.Equal(120.50m, items[0].Price);
        }

        [Fact]
        public void GetCartItems_CorruptedJson_RecoversSafelyWithoutThrowing()
        {
            // Arrange - simulate corrupted/truncated JSON (such as from a crash or aborted stream)
            var session = new MockSession();
            session.SetString("Cart", "{ broken JSON truncated [1, 2,");

            // Act
            var items = session.GetCartItems();

            // Assert
            Assert.NotNull(items);
            Assert.Empty(items);
            // Session key should be cleared
            Assert.Null(session.GetString("Cart"));
        }

        [Fact]
        public void CartHelper_GetCartCount_And_GetCartTotal_CalculateFromSessionAccurately()
        {
            // Arrange
            var session = new MockSession();
            session.SetCartItems(new List<CartItemDto>
            {
                new CartItemDto { ProductId = 1, Quantity = 3, Price = 50.00m },  // 150
                new CartItemDto { ProductId = 2, Quantity = 2, Price = 100.00m }  // 200
            });

            // Act
            int count = CartHelper.GetCartCount(session);
            decimal total = CartHelper.GetCartTotal(session);

            // Assert
            Assert.Equal(5, count);
            Assert.Equal(350.00m, total);
        }
    }
}
