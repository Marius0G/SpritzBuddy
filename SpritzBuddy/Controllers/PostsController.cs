using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using SpritzBuddy.Data;
using SpritzBuddy.Models;
using SpritzBuddy.Models.ViewModels;
using SpritzBuddy.Services;

namespace SpritzBuddy.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPostMediaService _postMediaService;
        private readonly IPostService _postService;
        private readonly IWebHostEnvironment _environment;

        public PostsController(ApplicationDbContext context, IPostMediaService postMediaService, IPostService postService, IWebHostEnvironment environment)
        {
            _context = context;
            _postMediaService = postMediaService;
            _postService = postService;
            _environment = environment;
        }

        // GET: Posts - Shows posts based on privacy and following relationships
        public async Task<IActionResult> Index()
        {
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.PostMedias)
                .Include(p => p.PostDrinks)
                    .ThenInclude(pd => pd.Drink)
                .AsQueryable();
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
                .OrderByDescending(p => p.Likes.Count)
                .ThenByDescending(p => p.CreateDate)
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
                .Include(p => p.PostMedias)
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
                .Include(p => p.PostMedias)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // GET: Posts/Create - Redirects to login if not authenticated
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var model = new CreatePostViewModel();
            
            // Populate available drinks for the dropdown
            var drinks = await _context.Drinks.OrderBy(d => d.Name).ToListAsync();
            model.AvailableDrinks = drinks.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Name
            }).ToList();
            
            return View(model);
        }

        // POST: Posts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(CreatePostViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                ModelState.AddModelError("", "Unable to determine current user. Please log in again.");
                // Repopulate drinks dropdown
                var drinksError = await _context.Drinks.OrderBy(d => d.Name).ToListAsync();
                model.AvailableDrinks = drinksError.Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name
                }).ToList();
                return View(model);
            }

            if (ModelState.IsValid)
            {
                // Create the post
                var post = new Post
                {
                    UserId = userIdInt,
                    Title = model.Title,
                    Content = model.Content,
                    CreateDate = DateTime.UtcNow
                };

                try
                {
                    _context.Add(post);
                    await _context.SaveChangesAsync();

                    // Handle media uploads if any
                    if (model.MediaFiles != null && model.MediaFiles.Any())
                    {
                        var uploadedPaths = await _postMediaService.UploadPostMediaAsync(model.MediaFiles, post.Id);

                        // Save media references to database
                        int orderIndex = 0;
                        foreach (var path in uploadedPaths)
                        {
                            var postMedia = new PostMedia
                            {
                                PostId = post.Id,
                                FilePath = path,
                                MediaType = PostMediaType.Image,
                                OrderIndex = orderIndex++
                            };
                            _context.PostMedias.Add(postMedia);
                        }

                        await _context.SaveChangesAsync();
                    }

                    // Handle drink tagging
                    if (model.SelectedDrinkIds != null && model.SelectedDrinkIds.Any())
                    {
                        foreach (var drinkId in model.SelectedDrinkIds)
                        {
                            var postDrink = new PostDrink
                            {
                                PostId = post.Id,
                                DrinkId = drinkId
                            };
                            _context.PostDrinks.Add(postDrink);
                        }

                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Post created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating post: {ex.Message}");
                }
            }
            else
            {
                // Log validation errors for debugging
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
            }

            // Repopulate drinks dropdown on error
            var drinks = await _context.Drinks.OrderBy(d => d.Name).ToListAsync();
            model.AvailableDrinks = drinks.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Name
            }).ToList();

            return View(model);
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

        // DIAGNOSTIC: Check post media status
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DiagnoseMedia(int postId)
        {
            var post = await _context.Posts
                .Include(p => p.PostMedias)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                return Content($"Post {postId} not found");

            var mediaCount = post.PostMedias?.Count ?? 0;
            var mediaInfo = post.PostMedias?.Select(pm => new
            {
                pm.Id,
                pm.FilePath,
                pm.OrderIndex,
                pm.MediaType,
                FileExists = System.IO.File.Exists(Path.Combine(_environment.WebRootPath, pm.FilePath.TrimStart('/')))
            }).ToList();

            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "posts");
            var filesInDirectory = Directory.Exists(uploadsPath) 
                ? Directory.GetFiles(uploadsPath).Select(f => Path.GetFileName(f)).ToList()
                : new List<string>();

            return Content($@"
POST MEDIA DIAGNOSTIC for Post #{postId}

Post Title: {post.Title}
PostMedias Count: {mediaCount}

Database Records:
{string.Join("\n", mediaInfo?.Select(m => $"  - ID: {m.Id}, Path: {m.FilePath}, Exists: {m.FileExists}") ?? new[] { "  (none)" })}

Files in uploads/posts directory:
{string.Join("\n", filesInDirectory.Select(f => $"  - {f}"))}

Upload Directory: {uploadsPath}
Directory Exists: {Directory.Exists(uploadsPath)}
            ");
        }

        // POST: Posts/ToggleLike - Toggle like for a post (Noroc!)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return Unauthorized();
            }

            try
            {
                var likeCount = await _postService.ToggleLikeAsync(id, userIdInt);
                return Ok(new { success = true, likeCount = likeCount });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
