using FoodShoppingApp.Data;
using FoodShoppingApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodShoppingApp.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Category
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(categories);
        }

        // GET: /Category/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                ModelState.AddModelError("Name", "Category name is required.");
            }
            else if (await _context.Categories.AnyAsync(c => c.Name.ToLower() == category.Name.Trim().ToLower()))
            {
                ModelState.AddModelError("Name", "A category with this name already exists.");
            }

            if (ModelState.IsValid)
            {
                category.Name = category.Name.Trim();
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Category '{category.Name}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: /Category/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: /Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(category.Name))
            {
                ModelState.AddModelError("Name", "Category name is required.");
            }
            else if (await _context.Categories.AnyAsync(c => c.Id != id && c.Name.ToLower() == category.Name.Trim().ToLower()))
            {
                ModelState.AddModelError("Name", "Another category with this name already exists.");
            }

            if (ModelState.IsValid)
            {
                category.Name = category.Name.Trim();
                _context.Update(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Category '{category.Name}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: /Category/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            ViewBag.HasProducts = category.Products?.Any() == true;
            return View(category);
        }

        // POST: /Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Protect products and order items against cascade deletion
            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                TempData["ErrorMessage"] = "Cannot delete this category because it contains active products. Please reassign or delete the products first.";
                return RedirectToAction(nameof(Index));
            }

            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Category '{category.Name}' was deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
