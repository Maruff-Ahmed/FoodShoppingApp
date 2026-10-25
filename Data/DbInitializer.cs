using FoodShoppingApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodShoppingApp.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(AppDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Check if connection string has placeholder server name
            var connStr = context.Database.GetDbConnection().ConnectionString;
            if (string.IsNullOrWhiteSpace(connStr) || connStr.Contains("YOUR_SERVER_NAME"))
            {
                return;
            }

            // Verify database can be reached
            if (!await context.Database.CanConnectAsync())
            {
                return;
            }

            // Ensure database tables & migrations are created
            try
            {
                await context.Database.MigrateAsync();
            }
            catch
            {
                await context.Database.EnsureCreatedAsync();
            }

            // Ensure Roles exist
            string[] roles = ["Admin", "Customer"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Admin User if not existing
            var adminEmail = "admin@foodshopping.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, "Admin@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // Seed Categories
            var categories = new Dictionary<string, Category>
            {
                ["Fresh Vegetables"] = new Category { Name = "Fresh Vegetables" },
                ["Organic Fruits"] = new Category { Name = "Organic Fruits" },
                ["Dairy & Eggs"] = new Category { Name = "Dairy & Eggs" },
                ["Bakery & Bread"] = new Category { Name = "Bakery & Bread" },
                ["Cold Beverages"] = new Category { Name = "Cold Beverages" },
                ["Meat & Seafood"] = new Category { Name = "Meat & Seafood" },
                ["Fast Food & Snacks"] = new Category { Name = "Fast Food & Snacks" }
            };

            foreach (var cat in categories.Values)
            {
                var existingCat = await context.Categories.FirstOrDefaultAsync(c => c.Name == cat.Name);
                if (existingCat == null)
                {
                    await context.Categories.AddAsync(cat);
                }
                else
                {
                    categories[cat.Name] = existingCat;
                }
            }
            await context.SaveChangesAsync();

            // Refresh loaded categories
            var allCats = await context.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);

            // Seed Products
            var productsToSeed = new List<Product>
            {
                new()
                {
                    Name = "Farm Fresh Organic Tomatoes (1 kg)",
                    CategoryId = allCats.GetValueOrDefault("Fresh Vegetables"),
                    Price = 90.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=600&auto=format&fit=crop&q=80",
                    Description = "Plump, vine-ripened organic red tomatoes harvested daily from local eco-farms. Perfect for fresh salads, sauces, and cooking."
                },
                new()
                {
                    Name = "Crisp Green Broccoli Crown (500g)",
                    CategoryId = allCats.GetValueOrDefault("Fresh Vegetables"),
                    Price = 120.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1584270354949-c26b0d5b4a0c?w=600&auto=format&fit=crop&q=80",
                    Description = "Nutrient-packed organic broccoli with firm florets, rich in vitamins C and K. Handpicked at peak freshness."
                },
                new()
                {
                    Name = "Crunchy Sweet Orange Carrots (1 kg)",
                    CategoryId = allCats.GetValueOrDefault("Fresh Vegetables"),
                    Price = 75.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1598170845058-32b9d6a5c317?w=600&auto=format&fit=crop&q=80",
                    Description = "Naturally sweet, earthy, soil-washed carrots packed with beta-carotene. Ideal for snacking, juicing, and soups."
                },
                new()
                {
                    Name = "Crisp Royal Gala Apples (1 kg)",
                    CategoryId = allCats.GetValueOrDefault("Organic Fruits"),
                    Price = 180.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=600&auto=format&fit=crop&q=80",
                    Description = "Aromatic, extra sweet, and delightfully juicy red apples. Naturally grown without artificial waxes or chemicals."
                },
                new()
                {
                    Name = "Ripe Golden Cavendish Bananas (1 Dozen)",
                    CategoryId = allCats.GetValueOrDefault("Organic Fruits"),
                    Price = 85.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=600&auto=format&fit=crop&q=80",
                    Description = "Rich in potassium and natural energy. Plump, sweet bananas ripened naturally for breakfast bowls and healthy snacks."
                },
                new()
                {
                    Name = "Fresh Sweet Garden Strawberries (250g)",
                    CategoryId = allCats.GetValueOrDefault("Organic Fruits"),
                    Price = 320.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?w=600&auto=format&fit=crop&q=80",
                    Description = "Vibrant red, fragrant, hand-picked seasonal strawberries bursting with sweet berry flavor and vitamin C."
                },
                new()
                {
                    Name = "Creamy Hass Avocado (2 pcs)",
                    CategoryId = allCats.GetValueOrDefault("Organic Fruits"),
                    Price = 260.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1523049673857-eb18f1d7b578?w=600&auto=format&fit=crop&q=80",
                    Description = "Buttery and rich ripe Hass avocados packed with healthy monounsaturated fats. Ideal for guacamole and breakfast toast."
                },
                new()
                {
                    Name = "Organic Whole Cow Milk (1 Litre)",
                    CategoryId = allCats.GetValueOrDefault("Dairy & Eggs"),
                    Price = 95.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1550583724-b2692b85b150?w=600&auto=format&fit=crop&q=80",
                    Description = "Pasteurized 100% pure whole milk from grass-fed cows. Rich, creamy, and chilled in hygienic glass packaging."
                },
                new()
                {
                    Name = "Farm Fresh Brown Eggs (Pack of 12)",
                    CategoryId = allCats.GetValueOrDefault("Dairy & Eggs"),
                    Price = 145.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?w=600&auto=format&fit=crop&q=80",
                    Description = "Pasture-raised farm eggs with vibrant golden yolks. Sourced from cage-free hens fed on natural organic grains."
                },
                new()
                {
                    Name = "Artisan Rustic Sourdough Bread (500g)",
                    CategoryId = allCats.GetValueOrDefault("Bakery & Bread"),
                    Price = 160.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1509440159596-0249088772ff?w=600&auto=format&fit=crop&q=80",
                    Description = "Slow-fermented artisan sourdough with an open airy crumb and a golden crunchy blistered crust. Freshly baked every dawn."
                },
                new()
                {
                    Name = "Golden Butter Flaky Croissant (2 pcs)",
                    CategoryId = allCats.GetValueOrDefault("Bakery & Bread"),
                    Price = 130.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1555507036-ab1f4038808a?w=600&auto=format&fit=crop&q=80",
                    Description = "Authentic French-style layered croissants crafted with pure European butter for light, melt-in-the-mouth perfection."
                },
                new()
                {
                    Name = "Cold-Pressed Fresh Orange Juice (1 Litre)",
                    CategoryId = allCats.GetValueOrDefault("Cold Beverages"),
                    Price = 210.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1621506289937-a8e4df240d0b?w=600&auto=format&fit=crop&q=80",
                    Description = "100% pure squeezed Valencia oranges with delicate pulp. Zero added sugar, no preservatives, and deeply refreshing."
                },
                new()
                {
                    Name = "Chilled Mineral Spring Water (1 Litre)",
                    CategoryId = allCats.GetValueOrDefault("Cold Beverages"),
                    Price = 25.00m,
                    ImageUrl = "/images/water.jpg",
                    Description = "Crisp, naturally filtered mountain spring water bottled with balanced essential minerals."
                },
                new()
                {
                    Name = "Atlantic Fresh Salmon Steak (500g)",
                    CategoryId = allCats.GetValueOrDefault("Meat & Seafood"),
                    Price = 750.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=600&auto=format&fit=crop&q=80",
                    Description = "Premium sushi-grade salmon cut into thick steaks. Loaded with heart-healthy omega-3 fatty acids."
                },
                new()
                {
                    Name = "Gourmet Double Cheeseburger",
                    CategoryId = allCats.GetValueOrDefault("Fast Food & Snacks"),
                    Price = 280.00m,
                    ImageUrl = "/images/cheeseburger.jpg",
                    Description = "Two juicy beef patties topped with melted cheddar, crisp lettuce, red onions, pickles, and our signature chef sauce."
                },
                new()
                {
                    Name = "Classic Pepperoni Pizza (10-inch)",
                    CategoryId = allCats.GetValueOrDefault("Fast Food & Snacks"),
                    Price = 540.00m,
                    ImageUrl = "/images/pizza.jpg",
                    Description = "Stone-baked thin crust pizza laden with rich tomato marinara, shredded mozzarella, and crispy pepperoni slices."
                },
                new()
                {
                    Name = "Golden Crispy French Fries",
                    CategoryId = allCats.GetValueOrDefault("Fast Food & Snacks"),
                    Price = 110.00m,
                    ImageUrl = "/images/fries.jpg",
                    Description = "Golden fried Russet potato fries seasoned lightly with sea salt and rosemary herbs. Delivered hot and crunchy."
                }
            };

            foreach (var prod in productsToSeed)
            {
                if (prod.CategoryId > 0)
                {
                    var exists = await context.Products.AnyAsync(p => p.Name == prod.Name);
                    if (!exists)
                    {
                        await context.Products.AddAsync(prod);
                    }
                }
            }
            await context.SaveChangesAsync();

            // Seed Promo Discounts
            var discounts = new List<Discount>
            {
                new() { Code = "SAVE10", Percentage = 10, IsActive = true },
                new() { Code = "FRESH20", Percentage = 20, IsActive = true },
                new() { Code = "ORGANIC15", Percentage = 15, IsActive = true }
            };

            foreach (var disc in discounts)
            {
                var exists = await context.Discounts.AnyAsync(d => d.Code == disc.Code);
                if (!exists)
                {
                    await context.Discounts.AddAsync(disc);
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
