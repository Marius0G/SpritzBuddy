using System.Collections.Generic;
using System.Threading.Tasks;
using SpritzBuddy.Models.ViewModels;

namespace SpritzBuddy.Services
{
    public interface IGamificationService
    {
        /// <summary>
        /// Calculates drink statistics for a user based on their post history.
        /// Returns a list of drink stats with names, percentages, and colors.
        /// </summary>
        Task<List<DrinkStatViewModel>> GetDrinkStatsAsync(int userId);

        /// <summary>
        /// Calculates and returns a list of badges earned by the user based on their activity.
        /// </summary>
        Task<List<BadgeViewModel>> GetUserBadgesAsync(int userId);
    }
}
