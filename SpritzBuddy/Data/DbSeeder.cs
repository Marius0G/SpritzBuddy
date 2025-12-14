using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
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
 }
}
