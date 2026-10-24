using FoodShoppingApp.Data;
using FoodShoppingApp.Helpers;
using FoodShoppingApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodShoppingApp.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        private List<CartItem> GetCart()
        {
            return HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetObject("Cart", cart);
        }

        // GET: /Cart/Index
        public IActionResult Index()
        {
            return View(GetCart());
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            if (quantity <= 0 || quantity > 50)
            {
                TempData["ErrorMessage"] = "Please choose a quantity between 1 and 50.";
                return RedirectToAction("Details", "Shop", new { id = productId });
            }

            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return NotFound();
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.Product.Id == productId);

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    Product = product,
                    Quantity = quantity
                });
            }
            else
            {
                item.Quantity = OrderCalculationHelper.ClampQuantity(item.Quantity + quantity);
            }

            SaveCart(cart);
            TempData["SuccessMessage"] = $"Added '{product.Name}' to your cart.";
            return RedirectToAction("Index");
        }

        // Quick GET Add for backwards compatibility
        [HttpGet]
        public IActionResult Add(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.Product.Id == id);

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    Product = product,
                    Quantity = 1
                });
            }
            else
            {
                item.Quantity = OrderCalculationHelper.ClampQuantity(item.Quantity + 1);
            }

            SaveCart(cart);
            TempData["SuccessMessage"] = $"Added '{product.Name}' to your cart.";
            return RedirectToAction("Index");
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.Product.Id == productId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.RemoveAll(c => c.Product.Id == productId);
                    TempData["SuccessMessage"] = "Item removed from cart.";
                }
                else
                {
                    item.Quantity = OrderCalculationHelper.ClampQuantity(quantity);
                }
                SaveCart(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            var removed = cart.RemoveAll(c => c.Product.Id == id);
            if (removed > 0)
            {
                SaveCart(cart);
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
