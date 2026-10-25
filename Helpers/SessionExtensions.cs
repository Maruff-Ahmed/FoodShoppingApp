using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FoodShoppingApp.Models;
using Microsoft.AspNetCore.Http;

namespace FoodShoppingApp.Helpers
{
    public static class SessionExtensions
    {
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            if (string.IsNullOrEmpty(value)) return default;

            try
            {
                return JsonSerializer.Deserialize<T>(value);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Reads minimal cart DTOs from session safely.
        /// Gracefully handles new CartItemDto format, legacy serialized Product format, and corrupted JSON.
        /// </summary>
        public static List<CartItemDto> GetCartItems(this ISession session)
        {
            var json = session.GetString("Cart");
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<CartItemDto>();
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return new List<CartItemDto>();
                }

                var result = new List<CartItemDto>();
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    int productId = 0;
                    int quantity = 1;
                    decimal price = 0m;

                    // 1. Direct DTO property: "ProductId"
                    if (element.TryGetProperty("ProductId", out var pidProp) && pidProp.TryGetInt32(out var pIdVal))
                    {
                        productId = pIdVal;
                    }
                    // 2. Legacy structure: "Product": { "Id": ..., "Price": ... }
                    else if (element.TryGetProperty("Product", out var prodProp) && prodProp.ValueKind == JsonValueKind.Object)
                    {
                        if (prodProp.TryGetProperty("Id", out var nestedId) && nestedId.TryGetInt32(out var nId))
                        {
                            productId = nId;
                        }
                        if (prodProp.TryGetProperty("Price", out var nestedPrice) && nestedPrice.TryGetDecimal(out var nPrice))
                        {
                            price = nPrice;
                        }
                    }

                    if (element.TryGetProperty("Quantity", out var qtyProp) && qtyProp.TryGetInt32(out var qVal))
                    {
                        quantity = OrderCalculationHelper.ClampQuantity(qVal);
                    }

                    if (element.TryGetProperty("Price", out var priceProp) && priceProp.TryGetDecimal(out var prVal))
                    {
                        price = prVal;
                    }

                    if (productId > 0)
                    {
                        var existing = result.FirstOrDefault(r => r.ProductId == productId);
                        if (existing != null)
                        {
                            existing.Quantity = OrderCalculationHelper.ClampQuantity(existing.Quantity + quantity);
                            if (price > 0) existing.Price = price;
                        }
                        else
                        {
                            result.Add(new CartItemDto
                            {
                                ProductId = productId,
                                Quantity = quantity,
                                Price = price
                            });
                        }
                    }
                }

                return result;
            }
            catch
            {
                // In case of corrupt or unparseable JSON (e.g. from prior cyclic serialization crash),
                // remove the broken session key and return a clean empty cart.
                try { session.Remove("Cart"); } catch { }
                return new List<CartItemDto>();
            }
        }

        /// <summary>
        /// Saves minimal cart representation to session. Never serializes complete EF Core entities.
        /// </summary>
        public static void SetCartItems(this ISession session, IEnumerable<CartItemDto> items)
        {
            var dtos = items?
                .Where(c => c.ProductId > 0 && c.Quantity > 0)
                .Select(c => new CartItemDto
                {
                    ProductId = c.ProductId,
                    Quantity = OrderCalculationHelper.ClampQuantity(c.Quantity),
                    Price = c.Price
                })
                .ToList() ?? new List<CartItemDto>();

            session.SetString("Cart", JsonSerializer.Serialize(dtos));
        }
    }
}
