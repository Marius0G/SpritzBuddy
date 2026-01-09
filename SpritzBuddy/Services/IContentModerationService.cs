using System.Threading.Tasks;

namespace SpritzBuddy.Services
{
    public interface IContentModerationService
    {
        /// <summary>
        /// Checks if the provided text contains inappropriate content.
        /// Returns true if content is safe, false if it contains prohibited terms.
        /// </summary>
        Task<bool> IsContentSafeAsync(string text);
    }
}
