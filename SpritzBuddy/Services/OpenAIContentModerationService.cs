using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace SpritzBuddy.Services
{
    public class OpenAIContentModerationService : IContentModerationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<OpenAIContentModerationService> _logger;
        
        // Lista extins? de cuvinte profane române?ti
        private readonly HashSet<string> _romanianProfanity = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Injurii directe
            "prost", "proasta", "prosti", "proaste", "prostule", "prostilor",
            "idiot", "idioata", "idioti", "idioate", "idiotule", "idiotilor",
            "tampit", "tampita", "tampiti", "tampite", "tâmpit", "tâmpit?",
            "cretin", "cretina", "cretini", "cretine", "cretinule",
            "pula", "pulii", "pulei", "puli", "pul?",
            "pizda", "pizdei", "pizde", "pizd?",
            "pula mea", "pula ta", "pula lui",
            "muie", "muist", "muisti", "mui?tii",
            "futut", "futu", "fututi", "futu?i", "futui", "futu-te", "futu-l", "futea",
            "fut", "futi", "fu?i", "futere",
            "cacat", "c?cat", "cacatu", "c?catu", "cacat de", "rahat",
            "dracu", "dracului", "naiba", "naibii",
            "curve", "curva", "curv?", "curvelor", "curvo",
            "tarfa", "tarfe", "târf?", "târfe",
            "puta", "pute", "pu?oi", "putoare",
            "jeg", "jegos", "jegoasa",
            "nenorocit", "nenoroci?i", "nenorocita", "nenorocitule",
            "ma-ta", "mata", "m?-ta", "mama ta",
            "mortii tai", "mor?ii t?i", "mor?ii ma-tii",
            
            // Variante scrise gre?it
            "pla", "plm", "plua", "pulã",
            "pzda", "pzd", "pzd?",
            "fmm", "fmm-", "fututi mortii",
            "mumu", "mumuie", "m00ie",
            "c4c4t", "kkt", "kktu",
            
            // Expresii vulgare
            "sugi pula", "suge pula", "sugipula",
            "ia-o in pizda", "ia-ti-o",
            "du-te in pizda ma-tii", "du-te-n pizda",
            "sa-mi sugi", "sa ma sugi",
            "te fut", "te-am futut", "sa te fut",
            "fut mortii", "fututi mortii",
            "in pula mea", "in pizda ma-tii",
            
            // Variante cenzurate
            "p*la", "p**a", "p***", "pu**",
            "pi*da", "p*zda", "p****",
            "f*t", "fu*", "f**t",
            "mu*e", "m*ie", "m**e"
        };

        public OpenAIContentModerationService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAIContentModerationService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
            _logger = logger;
            
            _logger.LogInformation($"OpenAI Content Moderation initialized with {_romanianProfanity.Count} Romanian profanity terms. API Key configured: {!string.IsNullOrEmpty(_apiKey)}");
        }

        public async Task<bool> IsContentSafeAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }
            
            // FIRST: Check Romanian profanity locally (fast)
            var normalizedText = text.ToLowerInvariant();
            normalizedText = RemoveDiacritics(normalizedText);
            
            foreach (var word in _romanianProfanity)
            {
                var normalizedWord = RemoveDiacritics(word.ToLowerInvariant());
                
                if (normalizedText.Contains(normalizedWord))
                {
                    _logger.LogWarning($"=== LOCAL FILTER: Romanian profanity detected ===");
                    _logger.LogWarning($"Text: '{text.Substring(0, Math.Min(100, text.Length))}'");
                    _logger.LogWarning($"Matched word: '{word}'");
                    return false;
                }
            }
            
            // SECOND: Check with OpenAI (for English and other languages)
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("OpenAI API Key missing - using only local Romanian filter");
                return true;
            }

            try 
            {
                _logger.LogDebug($"Checking content with OpenAI: {text.Substring(0, Math.Min(50, text.Length))}...");
                
                var requestBody = new
                {
                    input = text
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(HttpMethod.Post, "moderations")
                {
                    Content = content
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode) 
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"OpenAI API failed: {response.StatusCode} - {errorBody}");
                    return true;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                
                var results = doc.RootElement.GetProperty("results");
                if (results.GetArrayLength() > 0)
                {
                    var flagged = results[0].GetProperty("flagged").GetBoolean();
                    
                    if (flagged)
                    {
                        _logger.LogWarning($"=== OPENAI: Content flagged ===");
                        _logger.LogWarning($"Text: {text.Substring(0, Math.Min(100, text.Length))}...");
                        
                        var categories = results[0].GetProperty("categories");
                        var flaggedCategories = new List<string>();
                        
                        foreach (var category in categories.EnumerateObject())
                        {
                            if (category.Value.GetBoolean())
                            {
                                flaggedCategories.Add(category.Name);
                            }
                        }
                        
                        _logger.LogWarning($"Categories: {string.Join(", ", flaggedCategories)}");
                    }
                    
                    return !flagged;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI - using local filter only");
                return true;
            }
        }
        
        private static string RemoveDiacritics(string text)
        {
            return text
                .Replace("?", "a").Replace("â", "a")
                .Replace("î", "i").Replace("?", "s")
                .Replace("?", "t").Replace("?", "A")
                .Replace("Â", "A").Replace("Î", "I")
                .Replace("?", "S").Replace("?", "T");
        }
    }
}
