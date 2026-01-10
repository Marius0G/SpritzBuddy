using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Data;
using SpritzBuddy.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace SpritzBuddy.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context; // <--- 1. Adăugăm contextul bazei de date

        // 2. Injectăm contextul în constructor
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get current user ID
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            int? currentUserId = null;
            List<int> followingIds = new List<int>();

            if (userId != null && int.TryParse(userId, out int userIdInt))
            {
                currentUserId = userIdInt;
                
                // Get list of users that current user follows (accepted)
                followingIds = await _context.Follows
                    .Where(f => f.FollowerId == currentUserId.Value && f.Status == FollowStatus.Accepted)
                    .Select(f => f.FollowingId)
                    .ToListAsync();
            }

            // Load all posts with includes
            var allPosts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments.OrderBy(c => c.CreateDate).Take(3))
                    .ThenInclude(c => c.User)
                .Include(p => p.PostMedias)
                .Include(p => p.PostDrinks)
                    .ThenInclude(pd => pd.Drink)
                .ToListAsync();

            // Filter posts based on privacy settings
            var visiblePosts = allPosts.Where(p => 
                !p.User.IsPrivate || // Public accounts
                (currentUserId.HasValue && p.UserId == currentUserId.Value) || // Own posts
                (currentUserId.HasValue && followingIds.Contains(p.UserId)) // Following users
            ).ToList();

            // Sort: Followed users first (by date desc), then public posts (by date desc)
            var sortedPosts = visiblePosts
                .OrderByDescending(p => followingIds.Contains(p.UserId) ? 1 : 0) // Followed users first
                .ThenByDescending(p => p.CreateDate) // Then by date
                .ToList();

            return View(sortedPosts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // --- TEST PENTRU BAZA DE DATE ---
        [HttpGet]
        public IActionResult TestDatabase()
        {
            try
            {
                // Încercăm să vedem dacă ne putem conecta

                // Încercăm să numărăm utilizatorii (chiar dacă e 0, înseamnă că tabelul există)
                int userCount = _context.Users.Count();

                string? dbName = _context.Database.ProviderName;

                return Content($"SUCCES! ✅\n" +
                               $"M-am conectat la baza de date!\n" +
                               $"Provider: {dbName}\n" +
                               $"Număr utilizatori găsiți în tabelă: {userCount}");
            }
            catch (Exception ex)
            {
                // Dacă crapă ceva, afișăm eroarea pe ecran
                return Content($"EROARE CRITICĂ ❌\n\nMesaj: {ex.Message}\n\nDetalii: {ex.InnerException?.Message}");
            }
        }
        // -------------------------------

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}