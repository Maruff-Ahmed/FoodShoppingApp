using FoodShoppingApp.Data;
using FoodShoppingApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FoodShoppingApp.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // ----------------------
        // LIST PRODUCTS
        // ----------------------
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.Name)
                .ToListAsync();
            return View(products);
        }

        // ----------------------
        // CREATE PRODUCT - GET
        // ----------------------
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
            return View();
        }

        // ----------------------
        // CREATE PRODUCT - POST
        // ----------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (product.CategoryId == null || !await _context.Categories.AnyAsync(c => c.Id == product.CategoryId))
            {
                ModelState.AddModelError("CategoryId", "Please select a valid category.");
            }

            if (product.Price <= 0)
            {
                ModelState.AddModelError("Price", "Price must be greater than zero.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", product.CategoryId);
                return View(product);
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Product '{product.Name}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ----------------------
        // EDIT PRODUCT - GET
        // ----------------------
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // ----------------------
        // EDIT PRODUCT - POST
        // ----------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (product.CategoryId == null || !await _context.Categories.AnyAsync(c => c.Id == product.CategoryId))
            {
                ModelState.AddModelError("CategoryId", "Please select a valid category.");
            }

            if (product.Price <= 0)
            {
                ModelState.AddModelError("Price", "Price must be greater than zero.");
            }

            if (ModelState.IsValid)
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Product '{product.Name}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // ----------------------
        // DELETE PRODUCT - GET
        // ----------------------
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.HasOrderItems = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            return View(product);
        }

        // ----------------------
        // DELETE PRODUCT - POST
        // ----------------------
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Protect historical order items against cascade deletion
            var isReferencedInOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (isReferencedInOrders)
            {
                TempData["ErrorMessage"] = "Cannot delete this product because it exists in past order records. To retire this item, consider editing its name or description.";
                return RedirectToAction(nameof(Index));
            }

            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Product '{product.Name}' was deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
