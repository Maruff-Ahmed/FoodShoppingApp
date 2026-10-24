using FoodShoppingApp.Models;

namespace FoodShoppingApp.Helpers
{
    public static class OrderCalculationHelper
    {
        public static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Pending", "Processing", "Delivered", "Cancelled"
        };

        public const int MinItemQuantity = 1;
        public const int MaxItemQuantity = 50;

        /// <summary>
        /// Enforces quantity bounds between MinItemQuantity (1) and MaxItemQuantity (50).
        /// </summary>
        public static int ClampQuantity(int quantity)
        {
            if (quantity < MinItemQuantity) return MinItemQuantity;
            if (quantity > MaxItemQuantity) return MaxItemQuantity;
            return quantity;
        }

        /// <summary>
        /// Calculates the line item subtotal using the product price and quantity.
        /// </summary>
        public static decimal CalculateLineTotal(decimal price, int quantity)
        {
            if (price <= 0 || quantity <= 0) return 0m;
            return price * ClampQuantity(quantity);
        }

        /// <summary>
        /// Calculates the subtotal for a collection of cart items based on their product prices and quantities.
        /// </summary>
        public static decimal CalculateSubtotal(IEnumerable<CartItem>? items)
        {
            if (items == null) return 0m;
            return items.Sum(i => (i.Product?.Price ?? 0m) * ClampQuantity(i.Quantity));
        }

        /// <summary>
        /// Calculates the discount deduction amount based on active status and percentage.
        /// </summary>
        public static decimal CalculateDiscount(decimal subtotal, Discount? discount)
        {
            if (discount == null || !discount.IsActive || discount.Percentage <= 0)
            {
                return 0m;
            }

            var clampedPercentage = Math.Min(100, Math.Max(0, discount.Percentage));
            return Math.Round(subtotal * clampedPercentage / 100m, 2);
        }

        /// <summary>
        /// Calculates the final payable amount, guaranteeing it never drops below 0.
        /// </summary>
        public static decimal CalculateFinalTotal(decimal subtotal, decimal discountAmount)
        {
            return Math.Max(0m, subtotal - Math.Max(0m, discountAmount));
        }

        /// <summary>
        /// Validates whether an order fulfillment status is in the allowed whitelist.
        /// </summary>
        public static bool IsValidOrderStatus(string? status)
        {
            return !string.IsNullOrWhiteSpace(status) && AllowedStatuses.Contains(status.Trim());
        }
    }
}
