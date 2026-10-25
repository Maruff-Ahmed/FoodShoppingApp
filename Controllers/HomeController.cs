using FoodShoppingApp.Data;
using FoodShoppingApp.Models;
using FoodShoppingApp.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FoodShoppingApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel();

            try
            {
                viewModel.Categories = await _context.Categories
                    .Include(c => c.Products)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                viewModel.FeaturedProducts = await _context.Products
                    .Include(p => p.Category)
                    .Take(8)
                    .ToListAsync();

                viewModel.DealOfTheDayProducts = await _context.Products
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.Price)
                    .Take(4)
                    .ToListAsync();

                viewModel.TrendingProducts = await _context.Products
                    .Include(p => p.Category)
                    .OrderBy(p => p.Id)
                    .Take(6)
                    .ToListAsync();
            }
            catch
            {
                // Graceful fallback if database connection is pending
            }

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
