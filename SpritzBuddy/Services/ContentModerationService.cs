using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpritzBuddy.Services
{
    public class ContentModerationService : IContentModerationService
    {
        // Listă simplă de cuvinte interzise pentru demonstrație
        private readonly List<string> _bannedWords = new List<string>
        {
            "prost", "idiot", "tampit", "nebun", "urat", 
            "hate", "kill", "violence", "spam", "scam"
        };

        public async Task<bool> IsContentSafeAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            // Simulăm un mic delay ca și cum ar fi un apel API extern
            await Task.Delay(100);

            var normalizedText = text.ToLowerInvariant();

            foreach (var word in _bannedWords)
            {
                if (normalizedText.Contains(word))
                {
                    return false; // Conținut neadecvat găsit
                }
            }

            return true; // Conținut sigur
        }
    }
}
