using FoodShoppingApp.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace FoodShoppingApp.Helpers
{
    public static class CartHelper
    {
        public static int GetCartCount(ISession session)
        {
            var cart = session.GetCartItems();
            return cart?.Sum(c => c.Quantity) ?? 0;
        }

        public static decimal GetCartTotal(ISession session)
        {
            var cart = session.GetCartItems();
            return cart?.Sum(c => c.Price * c.Quantity) ?? 0m;
        }
    }
}
