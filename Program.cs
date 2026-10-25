using FoodShoppingApp.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// Add Services
// ----------------------

// Add DbContext with SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity for Admin/User
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Configure Authentication Cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";

    // ----- Development-friendly settings -----
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5); // cookie lifetime
    options.SlidingExpiration = false;               // don't auto-refresh
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add MVC
builder.Services.AddControllersWithViews();

// Add Session
builder.Services.AddSession();

var app = builder.Build();

// -------------------------------------------------------------
// ONE-TIME ADMINISTRATOR PASSWORD RESET UTILITY (TEMPORARY)
// Invoked strictly via CLI: dotnet run -- --reset-admin-password
// -------------------------------------------------------------
if (args.Contains("--reset-admin-password"))
{
    using var scope = app.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var targetEmail = "admin@foodapp.com";
    var tempPassword = "Admin@123456";

    Console.WriteLine($"[Admin Reset Utility] Searching for administrator account '{targetEmail}'...");
    try
    {
        var user = await userManager.FindByEmailAsync(targetEmail);
        if (user == null)
        {
            Console.WriteLine($"[ERROR] The administrator account '{targetEmail}' does not exist in the database. Reset aborted.");
            return;
        }

        var isAdmin = await userManager.IsInRoleAsync(user, "Admin");
        if (!isAdmin)
        {
            Console.WriteLine($"[ERROR] The account '{targetEmail}' exists but is not assigned the 'Admin' role. Reset aborted.");
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, token, tempPassword);

        if (resetResult.Succeeded)
        {
            await userManager.UpdateSecurityStampAsync(user);
            Console.WriteLine($"[SUCCESS] The password for administrator account '{targetEmail}' has been successfully reset. The account retains its 'Admin' role and all application data remains intact.");
        }
        else
        {
            Console.WriteLine("[ERROR] Password reset failed with the following errors:");
            foreach (var error in resetResult.Errors)
            {
                Console.WriteLine($" - {error.Description}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Could not connect to the database to perform password reset: {ex.Message}");
    }

    return; // Exit immediately without starting the web server
}

// ----------------------
// Configure Middleware
// ----------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Auto-seed initial grocery categories, products, and promo codes if database is connected
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await DbInitializer.SeedAsync(db, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "DbInitializer bypassed or skipped on startup.");
    }
}

app.Run();
