using FoodShoppingApp.Data;
using FoodShoppingApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodShoppingApp.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Discount/[action]/{id?}")]
    public class DiscountController : Controller
    {
        private readonly AppDbContext _context;

        public DiscountController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Discount/Index
        public async Task<IActionResult> Index()
        {
            var discounts = await _context.Discounts
                .OrderByDescending(d => d.Id)
                .ToListAsync();

            return View("~/Views/Admin/Discount/Index.cshtml", discounts);
        }

        // GET: /Admin/Discount/Create
        public IActionResult Create()
        {
            return View("~/Views/Admin/Discount/Create.cshtml", new Discount { IsActive = true, Percentage = 10 });
        }

        // POST: /Admin/Discount/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Discount discount)
        {
            if (string.IsNullOrWhiteSpace(discount.Code))
            {
                ModelState.AddModelError("Code", "Discount code is required.");
            }
            else if (await _context.Discounts.AnyAsync(d => d.Code.ToUpper() == discount.Code.Trim().ToUpper()))
            {
                ModelState.AddModelError("Code", "A discount code with this name already exists.");
            }

            if (discount.Percentage < 1 || discount.Percentage > 100)
            {
                ModelState.AddModelError("Percentage", "Discount percentage must be between 1% and 100%.");
            }

            if (ModelState.IsValid)
            {
                discount.Code = discount.Code.Trim().ToUpperInvariant();
                _context.Discounts.Add(discount);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Discount promo code '{discount.Code}' ({discount.Percentage}%) created.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Admin/Discount/Create.cshtml", discount);
        }

        // GET: /Admin/Discount/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
            {
                return NotFound();
            }

            return View("~/Views/Admin/Discount/Edit.cshtml", discount);
        }

        // POST: /Admin/Discount/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Discount discount)
        {
            if (id != discount.Id)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(discount.Code))
            {
                ModelState.AddModelError("Code", "Discount code is required.");
            }
            else if (await _context.Discounts.AnyAsync(d => d.Id != id && d.Code.ToUpper() == discount.Code.Trim().ToUpper()))
            {
                ModelState.AddModelError("Code", "Another discount code with this name already exists.");
            }

            if (discount.Percentage < 1 || discount.Percentage > 100)
            {
                ModelState.AddModelError("Percentage", "Discount percentage must be between 1% and 100%.");
            }

            if (ModelState.IsValid)
            {
                discount.Code = discount.Code.Trim().ToUpperInvariant();
                _context.Update(discount);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Discount promo code '{discount.Code}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Admin/Discount/Edit.cshtml", discount);
        }

        // POST: /Admin/Discount/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
            {
                return NotFound();
            }

            discount.IsActive = !discount.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Discount '{discount.Code}' status set to {(discount.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Discount/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount != null)
            {
                _context.Discounts.Remove(discount);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Discount '{discount.Code}' deleted.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
