using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Data;
using SpritzBuddy.Models;
using SpritzBuddy.Services;

var builder = WebApplication.CreateBuilder(args);

//1. CONFIGURARE BAZA DE DATE
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
 ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
 options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//2. CONFIGURARE IDENTITY
// Folosim ApplicationUser (pentru poză/descriere) și IdentityRole<int> (pentru Admin/User)
builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
{
 options.SignIn.RequireConfirmedAccount = false; // Nu cerem confirmare email momentan
 options.Password.RequireDigit = true;
 options.Password.RequiredLength =6;
 options.Password.RequireNonAlphanumeric = false; // Mai relaxat pentru teste
 options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI() // Ensure the built-in Identity Razor UI is registered
.AddDefaultTokenProviders();

// NOTA: Am șters linia cu "AddDefaultIdentity" care intra in conflict.

// Configure application cookies to redirect to custom login page
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login"; // Redirect to your custom login page
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied"; // Optional: custom access denied page
});

builder.Services.AddControllersWithViews();
// Register Razor Pages so Identity area pages (Areas/Identity) are reachable
builder.Services.AddRazorPages();

// Register profile service
builder.Services.AddScoped<IProfileService, ProfileService>();

// Register file upload service
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

// Register post media service
builder.Services.AddScoped<IPostMediaService, PostMediaService>();

// Register post service
builder.Services.AddScoped<IPostService, PostService>();

// Register group service
builder.Services.AddScoped<IGroupService, GroupService>();

// Register gamification service
builder.Services.AddScoped<IGamificationService, GamificationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
 app.UseMigrationsEndPoint();
}
else
{
 app.UseExceptionHandler("/Home/Error");
 app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
 name: "default",
 pattern: "{controller=Home}/{action=Index}/{id?}");
// Map Razor Pages endpoints (needed for Identity area pages)
app.MapRazorPages();

// --- BLOC NOU PENTRU DOCKER: Aplicare Migrări la Start ---
using (var scope = app.Services.CreateScope())
{
 var services = scope.ServiceProvider;
 try
 {
 var context = services.GetRequiredService<ApplicationDbContext>();
 // Această comandă face ce făcea "Update-Database" în consolă
 context.Database.Migrate();
 Console.WriteLine("✅ Baza de date a fost migrată cu succes în Docker!");

 // Seed roles using DbSeeder
 DbSeeder.SeedRolesAsync(services).GetAwaiter().GetResult();
 Console.WriteLine("✅ Roles seeding complete.");

 // Seed drinks using DbSeeder
 DbSeeder.SeedDrinksAsync(context).GetAwaiter().GetResult();
 Console.WriteLine("✅ Drinks seeding complete.");
 }
 catch (Exception ex)
 {
 var logger = services.GetRequiredService<ILogger<Program>>();
 logger.LogError(ex, "❌ Eroare la migrarea bazei de date.");
 }
}
// ---------------------------------------------------------

app.Run();
