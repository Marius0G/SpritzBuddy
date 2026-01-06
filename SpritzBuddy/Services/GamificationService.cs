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
        public async Task<List<string>> GetUserBadgesAsync(int userId)
        {
            var badges = new List<string>();

            // Check for "Newbie" badge: User has 1+ Post
            var postCount = await _context.Posts.CountAsync(p => p.UserId == userId);
            if (postCount >= 1)
            {
                badges.Add("Newbie");
            }

            // Check for "Social Butterfly" badge: User follows > 5 people
            var followingCount = await _context.Follows
                .CountAsync(f => f.FollowerId == userId && f.Status == Models.FollowStatus.Accepted);
            if (followingCount > 5)
            {
                badges.Add("Social Butterfly");
            }

            // Get all drinks tagged by the user for beer and spritz checks
            var userDrinks = await _context.PostDrinks
                .Include(pd => pd.Drink)
                .Include(pd => pd.Post)
                .Where(pd => pd.Post.UserId == userId)
                .Select(pd => pd.Drink.Name)
                .ToListAsync();

            // Check for "Beer Lover" badge: User has tagged "Bere" more than 5 times
            var beerCount = userDrinks.Count(d => d.Contains("Bere", StringComparison.OrdinalIgnoreCase));
            if (beerCount > 5)
            {
                badges.Add("Beer Lover");
            }

            // Check for "Spritz Master" badge: User has tagged "Aperol Spritz" or "Hugo" more than 5 times
            var spritzCount = userDrinks.Count(d => 
                d.Equals("Aperol Spritz", StringComparison.OrdinalIgnoreCase) || 
                d.Equals("Hugo", StringComparison.OrdinalIgnoreCase));
            if (spritzCount > 5)
            {
                badges.Add("Spritz Master");
            }

            // Check for "Influencer" badge: User has received > 10 Likes (Noroc) in total
            var totalLikes = await _context.Likes
                .CountAsync(l => l.Post.UserId == userId);
            if (totalLikes > 10)
            {
                badges.Add("Influencer");
            }

            return badges;
        }
    }
}
