using FoodShoppingApp.Data;
using FoodShoppingApp.Models;
using FoodShoppingApp.Helpers;
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
            return HttpContext.Session.GetObject<List<CartItem>>("Cart")
                   ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetObject("Cart", cart);
        }

        public IActionResult Index()
        {
            return View(GetCart());
        }

        public IActionResult Add(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();

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
                item.Quantity++;
            }

            SaveCart(cart);
            return RedirectToAction("Index", "Shop");
        }
        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return NotFound();

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
                item.Quantity += quantity;
            }

            SaveCart(cart);

            return RedirectToAction("Index");
        }


        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.Product.Id == id);
            SaveCart(cart);

            return RedirectToAction(nameof(Index));
        }
    }
}
