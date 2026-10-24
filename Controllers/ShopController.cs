using FoodShoppingApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodShoppingApp.Controllers
{
    public class ShopController : Controller
    {
        private readonly AppDbContext _context;

        public ShopController(AppDbContext context)
        {
            _context = context;
        }

        // USER PRODUCT LIST WITH SEARCH, FILTER, AND SORTING
        public async Task<IActionResult> Index(int? categoryId, string? search, string? sortBy)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p => p.Name.Contains(term) || (p.Description != null && p.Description.Contains(term)));
            }

            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name" => query.OrderBy(p => p.Name),
                _ => query.OrderBy(p => p.Name)
            };

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentSearch = search ?? string.Empty;
            ViewBag.CurrentSort = sortBy ?? "name";

            return View(await query.ToListAsync());
        }

        // PRODUCT DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Related products in the same category
            ViewBag.RelatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
                .Take(4)
                .ToListAsync();

            return View(product);
        }
    }
}
