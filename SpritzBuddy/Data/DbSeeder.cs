using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpritzBuddy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpritzBuddy.Data
{
 public static class DbSeeder
 {
 public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
 {
 if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

 var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

 var roles = new[] { "Administrator", "User", "Visitor" };

 foreach (var roleName in roles)
 {
 var exists = await roleManager.RoleExistsAsync(roleName);
 if (!exists)
 {
 var role = new IdentityRole<int> { Name = roleName, NormalizedName = roleName.ToUpperInvariant() };
 await roleManager.CreateAsync(role);
 }
 }
 }

 public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
 {
 if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

 var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
 
 // Check if admin user already exists
 var adminEmail = "admin@test.com";
 var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
 
 if (existingAdmin == null)
 {
 // Create admin user
 var adminUser = new ApplicationUser
 {
 UserName = "admin",
 Email = adminEmail,
 EmailConfirmed = true,
 FirstName = "Admin",
 LastName = "User",
 Description = "System Administrator",
 IsPrivate = false
 };
 
 var result = await userManager.CreateAsync(adminUser, "Admin123!");
 
 if (result.Succeeded)
 {
 // Add to Administrator role
 await userManager.AddToRoleAsync(adminUser, "Administrator");
 Console.WriteLine($"Admin user created successfully: {adminEmail}");
 }
 else
 {
 Console.WriteLine($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
 }
 }
 }

 public static async Task SeedDrinksAsync(ApplicationDbContext context)
 {
 if (context == null) throw new ArgumentNullException(nameof(context));

 // Check if drinks already exist
 if (await context.Drinks.AnyAsync())
 {
 return; // Drinks already seeded
 }

 var drinks = new List<Drink>
 {
 new Drink { Name = "Aperol Spritz", ColorHex = "#FF5500", AlcoholContent = 11 },
 new Drink { Name = "Hugo", ColorHex = "#20B2AA", AlcoholContent = 8 },
 new Drink { Name = "Bere Lager", ColorHex = "#FFD700", AlcoholContent = 5 },
 new Drink { Name = "Bere Neagră", ColorHex = "#3B1E08", AlcoholContent = 6 },
 new Drink { Name = "Vin Roșu", ColorHex = "#800000", AlcoholContent = 13 },
 new Drink { Name = "Vin Alb", ColorHex = "#F2E8C9", AlcoholContent = 12 },
 new Drink { Name = "Vin Rose", ColorHex = "#FFC0CB", AlcoholContent = 12 },
 new Drink { Name = "Gin Tonic", ColorHex = "#E0FFFF", AlcoholContent = 9 },
 new Drink { Name = "Mojito", ColorHex = "#00FF7F", AlcoholContent = 10 },
 new Drink { Name = "Cuba Libre", ColorHex = "#8B4513", AlcoholContent = 10 },
 new Drink { Name = "Tequila", ColorHex = "#F5F5DC", AlcoholContent = 40 },
 new Drink { Name = "Jägermeister", ColorHex = "#2F1E0E", AlcoholContent = 35 },
 new Drink { Name = "Whiskey Cola", ColorHex = "#1A1110", AlcoholContent = 15 },
 new Drink { Name = "Limonadă", ColorHex = "#FFFF00", AlcoholContent = 0 },
 new Drink { Name = "Apă Plată/Minerală", ColorHex = "#F0F8FF", AlcoholContent = 0 },
 new Drink { Name = "Cafea", ColorHex = "#6F4E37", AlcoholContent = 0 }
 };

 await context.Drinks.AddRangeAsync(drinks);
 await context.SaveChangesAsync();
 }
 }
}
