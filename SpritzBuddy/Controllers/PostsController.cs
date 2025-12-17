using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Data;
using SpritzBuddy.Models;

namespace SpritzBuddy.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Posts - Shows posts based on privacy and following relationships
        public async Task<IActionResult> Index()
        {
            var query = _context.Posts.Include(p => p.User).AsQueryable();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? currentUserId = null;
            
            if (userId != null && int.TryParse(userId, out int userIdInt))
            {
                currentUserId = userIdInt;
            }

            if (currentUserId.HasValue)
            {
                // User is logged in - show posts from public accounts + accounts they follow + their own posts
                var followingIds = await _context.Follows
                    .Where(f => f.FollowerId == currentUserId.Value && f.Status == FollowStatus.Accepted)
                    .Select(f => f.FollowingId)
                    .ToListAsync();

                query = query.Where(p => 
                    !p.User.IsPrivate ||                           // Public posts
                    p.UserId == currentUserId.Value ||             // Own posts
                    followingIds.Contains(p.UserId)                // Posts from followed users
                );
            }
            else
            {
                // User not logged in - show only public posts
                query = query.Where(p => !p.User.IsPrivate);
            }

            var allPosts = await query
                .OrderByDescending(p => p.CreateDate)
                .ToListAsync();

            ViewData["ShowingAllPosts"] = true;
            return View(allPosts);
        }

        [Authorize]
        public async Task<IActionResult> MyPosts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Account");
            }
            var userPosts = _context.Posts
                .Include(p => p.User)
                .Where(p => p.UserId == userIdInt)
                .OrderByDescending(p => p.CreateDate);

            ViewData["ShowingAllPosts"] = false;
            return View("Index", await userPosts.ToListAsync());
        }

        // GET: Posts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // GET: Posts/Create - Redirects to login if not authenticated
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Posts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Title,Content")] Post post)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                post.UserId = userIdInt;
                post.CreateDate = DateTime.UtcNow;
                
                _context.Add(post);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            return View(post);
        }

        // GET: Posts/Edit/5 - ONLY POST OWNER CAN ACCESS
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            // CHECK IF CURRENT USER OWNS THIS POST
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt) || post.UserId != userIdInt)
            {
                return Forbid(); // User doesn't own this post
            }

            return View(post);
        }

        // POST: Posts/Edit/5 - ONLY POST OWNER CAN EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,CreateDate")] Post post)
        {
            if (id != post.Id)
            {
                return NotFound();
            }

            // CHECK IF CURRENT USER OWNS THIS POST
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return Forbid();
            }

            // Verify ownership by checking the original post in database
            var originalPost = await _context.Posts.FindAsync(id);
            if (originalPost == null || originalPost.UserId != userIdInt)
            {
                return Forbid(); // User doesn't own this post
            }

            // Ensure UserId remains the same
            post.UserId = userIdInt;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(post);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(post.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            return View(post);
        }

        // GET: Posts/Delete/5 - ONLY POST OWNER CAN ACCESS
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            // CHECK IF CURRENT USER OWNS THIS POST
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt) || post.UserId != userIdInt)
            {
                return Forbid(); // User doesn't own this post
            }

            return View(post);
        }

        // POST: Posts/Delete/5 - ONLY POST OWNER CAN DELETE WITH CASCADE
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                // CHECK IF CURRENT USER OWNS THIS POST
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null || !int.TryParse(userId, out int userIdInt) || post.UserId != userIdInt)
                {
                    return Forbid(); // User doesn't own this post
                }

                // MANUAL CASCADE DELETE - Remove related data first
                // 1. Remove all comments for this post
                var comments = _context.Comments.Where(c => c.PostId == id);
                _context.Comments.RemoveRange(comments);

                // 2. Remove all likes for this post
                var likes = _context.Likes.Where(l => l.PostId == id);
                _context.Likes.RemoveRange(likes);

                // 3. PostMedia and PostDrinks are already cascade delete in DB, but we can be explicit
                var postMedias = _context.PostMedias.Where(pm => pm.PostId == id);
                _context.PostMedias.RemoveRange(postMedias);

                var postDrinks = _context.PostDrinks.Where(pd => pd.PostId == id);
                _context.PostDrinks.RemoveRange(postDrinks);

                // 4. Finally remove the post itself
                _context.Posts.Remove(post);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.Id == id);
        }
    }
}
