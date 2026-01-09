using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Data;
using SpritzBuddy.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpritzBuddy.Services
{
    public class GamificationService : IGamificationService
    {
        private readonly ApplicationDbContext _context;

        public GamificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Calculates drink statistics for a user based on their post history.
        /// Returns a list of drink stats with names, percentages, and colors.
        /// </summary>
        public async Task<List<DrinkStatViewModel>> GetDrinkStatsAsync(int userId)
        {
            // Get all drinks tagged by the user across all their posts
            var userDrinks = await _context.PostDrinks
                .Include(pd => pd.Drink)
                .Include(pd => pd.Post)
                .Where(pd => pd.Post.UserId == userId)
                .ToListAsync();

            var totalDrinks = userDrinks.Count;

            if (totalDrinks == 0)
            {
                return new List<DrinkStatViewModel>();
            }

            // Group by drink and calculate percentages with colors
            var drinkStats = userDrinks
                .GroupBy(pd => new { pd.Drink.Name, pd.Drink.ColorHex })
                .Select(g => new DrinkStatViewModel
                {
                    DrinkName = g.Key.Name,
                    ColorHex = g.Key.ColorHex ?? "#6c757d",
                    Percentage = Math.Round((double)g.Count() / totalDrinks * 100, 2)
                })
                .OrderByDescending(d => d.Percentage)
                .ToList();

            return drinkStats;
        }

        /// <summary>
        /// Calculates and returns a list of badges earned by the user based on their activity.
        /// </summary>
        public async Task<List<BadgeViewModel>> GetUserBadgesAsync(int userId)
        {
            var allBadges = new List<BadgeViewModel>
            {
                new BadgeViewModel { Name = "Newbie", Description = "Prima postare creată", IconClass = "bi-star-fill", ColorClass = "text-warning" },
                new BadgeViewModel { Name = "Social Butterfly", Description = "Urmărești mai mult de 5 persoane", IconClass = "bi-people-fill", ColorClass = "text-primary" },
                new BadgeViewModel { Name = "Beer Lover", Description = "Peste 5 beri etichetate", IconClass = "bi-cup-straw", ColorClass = "text-info" },
                new BadgeViewModel { Name = "Spritz Master", Description = "Peste 5 Spritz-uri savurate", IconClass = "bi-glass-margin", ColorClass = "text-danger" },
                new BadgeViewModel { Name = "Influencer", Description = "Ai primit peste 10 like-uri", IconClass = "bi-lightning-fill", ColorClass = "text-success" }
            };

            // Check for "Newbie" badge: User has 1+ Post
            var postCount = await _context.Posts.CountAsync(p => p.UserId == userId);
            
            // Check for "Social Butterfly" badge: User follows > 5 people
            var followingCount = await _context.Follows
                .CountAsync(f => f.FollowerId == userId && f.Status == Models.FollowStatus.Accepted);

            // Get all drinks tagged by the user for beer and spritz checks
            var userDrinks = await _context.PostDrinks
                .Include(pd => pd.Drink)
                .Include(pd => pd.Post)
                .Where(pd => pd.Post.UserId == userId)
                .Select(pd => pd.Drink.Name)
                .ToListAsync();

            // Check for "Beer Lover" badge: User has tagged "Bere" more than 5 times
            var beerCount = userDrinks.Count(d => d.Contains("Bere", StringComparison.OrdinalIgnoreCase));

            // Check for "Spritz Master" badge: User has tagged "Aperol Spritz" or "Hugo" more than 5 times
            var spritzCount = userDrinks.Count(d => 
                d.Equals("Aperol Spritz", StringComparison.OrdinalIgnoreCase) || 
                d.Equals("Hugo", StringComparison.OrdinalIgnoreCase));

            // Check for "Influencer" badge: User has received > 10 Likes (Noroc) in total
            var totalLikes = await _context.Likes
                .CountAsync(l => l.Post.UserId == userId);

            foreach (var badge in allBadges)
            {
                switch (badge.Name)
                {
                    case "Newbie": badge.IsUnlocked = postCount >= 1; break;
                    case "Social Butterfly": badge.IsUnlocked = followingCount > 5; break;
                    case "Beer Lover": badge.IsUnlocked = beerCount > 5; break;
                    case "Spritz Master": badge.IsUnlocked = spritzCount > 5; break;
                    case "Influencer": badge.IsUnlocked = totalLikes > 10; break;
                }
            }

            return allBadges;
        }
    }
}
