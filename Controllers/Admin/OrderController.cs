using FoodShoppingApp.Data;
using FoodShoppingApp.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodShoppingApp.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Order/[action]/{id?}")]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Order/Index
        public async Task<IActionResult> Index(string? status, string? search)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(o => o.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                if (int.TryParse(term.Replace("#", ""), out var searchId))
                {
                    query = query.Where(o => o.Id == searchId || (o.User != null && o.User.Email!.Contains(term)));
                }
                else
                {
                    query = query.Where(o => o.User != null && o.User.Email!.Contains(term));
                }
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.CurrentStatus = status ?? "All";
            ViewBag.CurrentSearch = search ?? string.Empty;

            return View("~/Views/Admin/Order/Index.cshtml", orders);
        }

        // GET: /Admin/Order/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View("~/Views/Admin/Order/Details.cshtml", order);
        }

        // POST: /Admin/Order/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            if (!OrderCalculationHelper.IsValidOrderStatus(status))
            {
                TempData["ErrorMessage"] = "Invalid order status provided.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status.Trim();
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order #{order.Id} status updated to '{order.Status}'.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
