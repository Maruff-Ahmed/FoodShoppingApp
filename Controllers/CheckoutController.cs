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
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(AppDbContext context, UserManager<IdentityUser> userManager, ILogger<CheckoutController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // ----------------------
        // CHECKOUT PAGE
        // ----------------------
        public async Task<IActionResult> Index()
        {
            var sessionCart = HttpContext.Session.GetCartItems();

            if (sessionCart == null || !sessionCart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Sync cart items with current database prices
            var verifiedCart = await BuildVerifiedCartAsync(sessionCart);
            if (!verifiedCart.Any())
            {
                HttpContext.Session.Remove("Cart");
                TempData["ErrorMessage"] = "The products in your cart are no longer available.";
                return RedirectToAction("Index", "Cart");
            }

            // Update session with verified minimal DTOs (never serializing EF Core entity graphs)
            var verifiedSessionItems = verifiedCart.Select(c => new CartItemDto
            {
                ProductId = c.Product.Id,
                Quantity = c.Quantity,
                Price = c.Product.Price
            }).ToList();
            HttpContext.Session.SetCartItems(verifiedSessionItems);

            var subtotal = OrderCalculationHelper.CalculateSubtotal(verifiedCart);
            var checkoutToken = Guid.NewGuid().ToString("N");
            HttpContext.Session.SetString("CheckoutToken", checkoutToken);

            var model = new CheckoutViewModel
            {
                CartItems = verifiedCart,
                SubTotal = subtotal,
                DiscountCode = null,
                DiscountAmount = 0m,
                FinalTotal = subtotal,
                CheckoutToken = checkoutToken
            };

            return View(model);
        }

        // ----------------------
        // APPLY DISCOUNT
        // ----------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyDiscount(CheckoutViewModel model)
        {
            var sessionCart = HttpContext.Session.GetCartItems();

            if (sessionCart == null || !sessionCart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var verifiedCart = await BuildVerifiedCartAsync(sessionCart);
            if (!verifiedCart.Any())
            {
                HttpContext.Session.Remove("Cart");
                TempData["ErrorMessage"] = "The products in your cart are no longer available.";
                return RedirectToAction("Index", "Cart");
            }

            var subtotal = OrderCalculationHelper.CalculateSubtotal(verifiedCart);
            decimal discountAmount = 0m;
            string? appliedCode = null;

            if (!string.IsNullOrWhiteSpace(model.DiscountCode))
            {
                var codeNormalized = model.DiscountCode.Trim();
                var discount = await _context.Discounts
                    .FirstOrDefaultAsync(d => d.Code == codeNormalized && d.IsActive);

                discountAmount = OrderCalculationHelper.CalculateDiscount(subtotal, discount);
                if (discountAmount > 0 && discount != null)
                {
                    appliedCode = discount.Code;
                    ViewBag.DiscountSuccess = $"Promo code '{discount.Code}' applied ({discount.Percentage}% OFF)!";
                }
                else
                {
                    ViewBag.DiscountError = "Invalid, expired, or inactive discount code.";
                }
            }

            var token = HttpContext.Session.GetString("CheckoutToken");
            if (string.IsNullOrEmpty(token))
            {
                token = Guid.NewGuid().ToString("N");
                HttpContext.Session.SetString("CheckoutToken", token);
            }

            model.CartItems = verifiedCart;
            model.SubTotal = subtotal;
            model.DiscountCode = appliedCode;
            model.DiscountAmount = discountAmount;
            model.FinalTotal = OrderCalculationHelper.CalculateFinalTotal(subtotal, discountAmount);
            model.CheckoutToken = token;

            return View("Index", model);
        }

        // ----------------------
        // PLACE ORDER (COD)
        // ----------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            // Idempotency token check to prevent duplicate / concurrent submissions
            var sessionToken = HttpContext.Session.GetString("CheckoutToken");
            if (string.IsNullOrEmpty(sessionToken) || sessionToken != model.CheckoutToken)
            {
                TempData["ErrorMessage"] = "This order was already submitted or your checkout session expired.";
                return RedirectToAction("MyOrders");
            }

            var sessionCart = HttpContext.Session.GetCartItems();

            if (sessionCart == null || !sessionCart.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Recalculate everything strictly server-side from current database state
            var productIds = sessionCart.Select(c => c.ProductId).Distinct().ToList();
            var dbProducts = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var orderItems = new List<OrderItem>();
            decimal subtotal = 0m;

            foreach (var item in sessionCart)
            {
                if (!dbProducts.TryGetValue(item.ProductId, out var dbProduct))
                {
                    TempData["ErrorMessage"] = "A product in your cart is no longer available.";
                    return RedirectToAction("Index", "Cart");
                }

                if (item.Quantity <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid item quantity.";
                    return RedirectToAction("Index", "Cart");
                }

                var unitPrice = dbProduct.Price;
                var itemQuantity = OrderCalculationHelper.ClampQuantity(item.Quantity);

                subtotal += unitPrice * itemQuantity;

                orderItems.Add(new OrderItem
                {
                    ProductId = dbProduct.Id,
                    Quantity = itemQuantity,
                    Price = unitPrice
                });
            }

            // Server-side discount calculation
            decimal discountAmount = 0m;
            string? verifiedDiscountCode = null;

            if (!string.IsNullOrWhiteSpace(model.DiscountCode))
            {
                var codeNormalized = model.DiscountCode.Trim();
                var discount = await _context.Discounts
                    .FirstOrDefaultAsync(d => d.Code == codeNormalized && d.IsActive);

                discountAmount = OrderCalculationHelper.CalculateDiscount(subtotal, discount);
                if (discountAmount > 0 && discount != null)
                {
                    verifiedDiscountCode = discount.Code;
                }
            }

            var finalTotal = OrderCalculationHelper.CalculateFinalTotal(subtotal, discountAmount);

            // Execute within a database transaction for data consistency
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    UserId = user.Id,
                    OrderDate = DateTime.UtcNow,
                    Status = "Pending",
                    PaymentMethod = "Cash On Delivery",
                    DiscountCode = verifiedDiscountCode ?? string.Empty,
                    DiscountAmount = discountAmount,
                    TotalAmount = finalTotal,
                    OrderItems = orderItems
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Clear the session cart and checkout token only on successful commit
                HttpContext.Session.Remove("CheckoutToken");
                HttpContext.Session.Remove("Cart");

                TempData["SuccessMessage"] = $"Thank you! Your order #{order.Id} has been placed successfully.";
                return RedirectToAction("Invoice", new { id = order.Id });
            }
            catch (Exception ex)
            {
                try { await transaction.RollbackAsync(); } catch { }
                _logger.LogError(ex, "An error occurred while placing order for user {UserId}", user.Id);
                TempData["ErrorMessage"] = "Could not place order: " + (ex.InnerException?.Message ?? ex.Message);
                return RedirectToAction("Index");
            }
        }

        // ----------------------
        // INVOICE (USER BILLING)
        // ----------------------
        public async Task<IActionResult> Invoice(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id && (o.UserId == user.Id || User.IsInRole("Admin")));

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // ----------------------
        // USER MY ORDERS
        // ----------------------
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // Helper to reconstruct verified cart from DB using minimal session DTOs
        private async Task<List<CartItem>> BuildVerifiedCartAsync(List<CartItemDto> sessionCart)
        {
            var productIds = sessionCart.Select(c => c.ProductId).Distinct().ToList();
            var dbProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var verifiedList = new List<CartItem>();
            foreach (var item in sessionCart)
            {
                if (dbProducts.TryGetValue(item.ProductId, out var dbProduct))
                {
                    verifiedList.Add(new CartItem
                    {
                        Product = dbProduct,
                        Quantity = OrderCalculationHelper.ClampQuantity(item.Quantity)
                    });
                }
            }

            return verifiedList;
        }
    }
}
