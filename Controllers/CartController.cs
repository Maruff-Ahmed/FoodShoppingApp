using FoodShoppingApp.Data;
using FoodShoppingApp.Helpers;
using FoodShoppingApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodShoppingApp.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Cart/Index
        // Retrieves current product details and authoritative prices from the database
        public async Task<IActionResult> Index()
        {
            var sessionCart = HttpContext.Session.GetCartItems();
            if (!sessionCart.Any())
            {
                return View(new List<CartItem>());
            }

            var productIds = sessionCart.Select(c => c.ProductId).Distinct().ToList();
            var dbProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var cartItems = new List<CartItem>();
            var verifiedSessionItems = new List<CartItemDto>();

            foreach (var item in sessionCart)
            {
                if (dbProducts.TryGetValue(item.ProductId, out var dbProduct))
                {
                    var clampedQty = OrderCalculationHelper.ClampQuantity(item.Quantity);
                    cartItems.Add(new CartItem
                    {
                        Product = dbProduct,
                        Quantity = clampedQty
                    });

                    // Sync authoritative database price to minimal session DTO
                    verifiedSessionItems.Add(new CartItemDto
                    {
                        ProductId = dbProduct.Id,
                        Quantity = clampedQty,
                        Price = dbProduct.Price
                    });
                }
            }

            // Sync cleaned/verified DTOs back to session (pruning deleted products)
            HttpContext.Session.SetCartItems(verifiedSessionItems);

            return View(cartItems);
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            if (quantity <= 0 || quantity > 50)
            {
                TempData["ErrorMessage"] = "Please choose a quantity between 1 and 50.";
                return RedirectToAction("Details", "Shop", new { id = productId });
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                return NotFound();
            }

            var sessionCart = HttpContext.Session.GetCartItems();
            var item = sessionCart.FirstOrDefault(c => c.ProductId == productId);

            if (item == null)
            {
                sessionCart.Add(new CartItemDto
                {
                    ProductId = product.Id,
                    Quantity = quantity,
                    Price = product.Price
                });
            }
            else
            {
                item.Quantity = OrderCalculationHelper.ClampQuantity(item.Quantity + quantity);
                item.Price = product.Price;
            }

            HttpContext.Session.SetCartItems(sessionCart);
            TempData["SuccessMessage"] = $"Added '{product.Name}' to your cart.";
            return RedirectToAction("Index");
        }

        // Quick GET Add for backwards compatibility
        [HttpGet]
        public async Task<IActionResult> Add(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            var sessionCart = HttpContext.Session.GetCartItems();
            var item = sessionCart.FirstOrDefault(c => c.ProductId == id);

            if (item == null)
            {
                sessionCart.Add(new CartItemDto
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    Price = product.Price
                });
            }
            else
            {
                item.Quantity = OrderCalculationHelper.ClampQuantity(item.Quantity + 1);
                item.Price = product.Price;
            }

            HttpContext.Session.SetCartItems(sessionCart);
            TempData["SuccessMessage"] = $"Added '{product.Name}' to your cart.";
            return RedirectToAction("Index");
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            var sessionCart = HttpContext.Session.GetCartItems();
            var item = sessionCart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    sessionCart.RemoveAll(c => c.ProductId == productId);
                    TempData["SuccessMessage"] = "Item removed from cart.";
                }
                else
                {
                    item.Quantity = OrderCalculationHelper.ClampQuantity(quantity);
                }
                HttpContext.Session.SetCartItems(sessionCart);
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int id)
        {
            var sessionCart = HttpContext.Session.GetCartItems();
            var removed = sessionCart.RemoveAll(c => c.ProductId == id);
            if (removed > 0)
            {
                HttpContext.Session.SetCartItems(sessionCart);
                TempData["SuccessMessage"] = "Item removed from cart.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove("Cart");
            TempData["SuccessMessage"] = "Shopping cart has been cleared.";
            return RedirectToAction(nameof(Index));
        }
    }
}
