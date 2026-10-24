using FoodShoppingApp.Data;
using FoodShoppingApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FoodShoppingApp.Controllers.Admin
{
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public AdminController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: /Admin/Register
        // Only existing administrators can access the registration form.
        [Authorize(Roles = "Admin")]
        [HttpGet("Register")]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Admin/Register
        // Only existing administrators can register additional administrator accounts.
        [Authorize(Roles = "Admin")]
        [HttpPost("Register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "An account with this email already exists.");
                return View(model);
            }

            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            // Ensure the Admin role exists
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                var roleCreateResult = await _roleManager.CreateAsync(new IdentityRole("Admin"));
                if (!roleCreateResult.Succeeded)
                {
                    var cleanupResult = await _userManager.DeleteAsync(user);
                    if (!cleanupResult.Succeeded)
                    {
                        ModelState.AddModelError(string.Empty, "Failed to create administrator role and failed to clean up the user account. Please contact system support.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Failed to create administrator role. Administrator account was not created.");
                    }
                    return View(model);
                }
            }

            // Assign user to Admin role
            var roleAssignResult = await _userManager.AddToRoleAsync(user, "Admin");
            if (!roleAssignResult.Succeeded)
            {
                var cleanupResult = await _userManager.DeleteAsync(user);
                if (!cleanupResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Failed to assign administrator role and failed to clean up the user account. Please contact system support.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Failed to assign administrator role. Administrator account was not created.");
                }
                return View(model);
            }

            // Success is only recorded and displayed after role assignment has succeeded
            TempData["SuccessMessage"] = $"Administrator account '{model.Email}' was created successfully.";
            return RedirectToAction("Dashboard");
        }

        // GET: /Admin/Login
        [HttpGet("Login")]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            {
                return RedirectToAction("Dashboard");
            }

            return View();
        }

        // POST: /Admin/Login
        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            // Verify user exists AND is in the Admin role before signing in
            if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToAction("Dashboard");

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        // GET: /Admin/Dashboard
        [Authorize(Roles = "Admin")]
        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                TotalOrders = await _context.Orders.CountAsync(),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending"),
                DeliveredOrders = await _context.Orders.CountAsync(o => o.Status == "Delivered"),
                TotalRevenue = await _context.Orders.Where(o => o.Status != "Cancelled").SumAsync(o => (decimal?)o.TotalAmount) ?? 0m,
                TotalProducts = await _context.Products.CountAsync(),
                TotalCategories = await _context.Categories.CountAsync(),
                ActiveDiscounts = await _context.Discounts.CountAsync(d => d.IsActive),
                RecentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }

        // POST: /Admin/Logout
        [Authorize(Roles = "Admin")]
        [HttpPost("Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
