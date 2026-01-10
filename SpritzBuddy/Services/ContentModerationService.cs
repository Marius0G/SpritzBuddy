using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpritzBuddy.Services
{
    public class ContentModerationService : IContentModerationService
    {
        // Listă extinsă de cuvinte interzise
        private readonly List<string> _bannedWords = new List<string>
        {
            // Injurii românești
            "prost", "proasta", "prosti", "proaste",
            "idiot", "idioata", "idioti", "idioate",
            "tampit", "tampita", "tampiti", "tampite",
            "nebun", "nebuna", "nebuni", "nebune",
            "urat", "urata", "urati", "urate",
            "pula", "pizda", "muie", "futut", "futu",
            "cacat", "rahat", "dracu", "dracului",
            "curve", "curva", "curvă", "curvele",
            "tarfa", "tarfe", "tâmpit", "tâmpită",
            "fraier", "fraieri", "prostanac", "proștii",
            
            // English profanity
            "fuck", "fucking", "shit", "damn", "bitch",
            "asshole", "bastard", "crap", "dick", "pussy",
            "cunt", "whore", "slut", "fag", "retard",
            
            // Hate speech
            "hate", "kill", "death", "murder", "destroy",
            "violence", "terrorist", "rape", "torture",
            
            // Spam related
            "spam", "scam", "phishing", "viagra", "casino",
            "lottery", "winner", "claim prize", "click here",
            
            // Harassment
            "kys", "kill yourself", "suicide", "die", "hang yourself"
        };

        public async Task<bool> IsContentSafeAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            // Simulăm un mic delay ca și cum ar fi un apel API extern
            await Task.Delay(50);

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
