using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SpritzBuddy.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Comments
        public async Task<IActionResult> Comments(string filter = "all", int page = 1)
        {
            int pageSize = 20;
            
            var query = _context.Comments
                .Include(c => c.User)
                .Include(c => c.Post)
                    .ThenInclude(p => p.User)
                .AsQueryable();

            // Filter by sentiment
            switch (filter.ToLower())
            {
                case "negative":
                    query = query.Where(c => c.SentimentLabel == "negative");
                    break;
                case "neutral":
                    query = query.Where(c => c.SentimentLabel == "neutral");
                    break;
                case "positive":
                    query = query.Where(c => c.SentimentLabel == "positive");
                    break;
                case "unanalyzed":
                    query = query.Where(c => c.SentimentLabel == null);
                    break;
                // "all" - no filter
            }

            var totalComments = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalComments / (double)pageSize);

            var comments = await query
                .OrderByDescending(c => c.SentimentConfidence ?? 0)
                .ThenByDescending(c => c.CreateDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentFilter = filter;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalComments = totalComments;

            // Statistics
            ViewBag.NegativeCount = await _context.Comments.CountAsync(c => c.SentimentLabel == "negative");
            ViewBag.NeutralCount = await _context.Comments.CountAsync(c => c.SentimentLabel == "neutral");
            ViewBag.PositiveCount = await _context.Comments.CountAsync(c => c.SentimentLabel == "positive");
            ViewBag.UnanalyzedCount = await _context.Comments.CountAsync(c => c.SentimentLabel == null);

            return View(comments);
        }

        // GET: Admin/Index
        public IActionResult Index()
        {
            return RedirectToAction("Comments");
        }
    }
}
