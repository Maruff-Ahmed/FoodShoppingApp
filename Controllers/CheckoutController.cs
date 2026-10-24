using FoodShoppingApp.Data;
using FoodShoppingApp.Helpers;
using FoodShoppingApp.Models;
using FoodShoppingApp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodShoppingApp.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CheckoutController(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ----------------------
        // CHECKOUT PAGE
        // ----------------------
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart");

            if (cart == null || !cart.Any())
                return RedirectToAction("Index", "Cart");

            var subtotal = cart.Sum(c => c.Product.Price * c.Quantity);

            var model = new CheckoutViewModel
            {
                CartItems = cart,
                SubTotal = subtotal,
                FinalTotal = subtotal
            };

            return View(model);
        }

        // ----------------------
        // APPLY DISCOUNT
        // ----------------------
        [HttpPost]
        public IActionResult ApplyDiscount(CheckoutViewModel model)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart");

            if (cart == null || !cart.Any())
                return RedirectToAction("Index", "Cart");

            var subtotal = cart.Sum(c => c.Product.Price * c.Quantity);

            var discount = _context.Discounts
                .FirstOrDefault(d => d.Code == model.DiscountCode && d.IsActive);

            decimal discountAmount = 0;

            if (discount != null)
            {
                discountAmount = subtotal * discount.Percentage / 100;
            }

            model.CartItems = cart;
            model.SubTotal = subtotal;
            model.DiscountAmount = discountAmount;
            model.FinalTotal = subtotal - discountAmount;

            return View("Index", model);
        }

        // ----------------------
        // PLACE ORDER (COD)
        // ----------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart");

            if (cart == null || !cart.Any())
                return RedirectToAction("Index", "Cart");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                Status = "Pending",
                PaymentMethod = "Cash On Delivery",
                DiscountCode = model.DiscountCode,
                DiscountAmount = model.DiscountAmount,
                TotalAmount = model.FinalTotal,
                OrderItems = cart.Select(c => new OrderItem
                {
                    ProductId = c.Product.Id,
                    Quantity = c.Quantity,
                    Price = c.Product.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Clear cart
            HttpContext.Session.Remove("Cart");

            // Redirect to Invoice
            return RedirectToAction("Invoice", new { id = order.Id });
        }

        // ----------------------
        // INVOICE (USER BILLING)
        // ----------------------
        public async Task<IActionResult> Invoice(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // ----------------------
        // USER MY ORDERS
        // ----------------------
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }
    }
}
