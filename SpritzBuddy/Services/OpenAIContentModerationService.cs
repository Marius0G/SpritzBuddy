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

        public OpenAIContentModerationService(
            IConfiguration configuration, 
            ILogger<OpenAIContentModerationService> logger)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI:ApiKey not configured");
            _logger = logger;
            
            // Configurare HttpClient pentru OpenAI API
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<bool> IsContentSafeAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            try 
            {
                _logger.LogInformation($"[MODERATION] Checking content (length: {text.Length}): {text.Substring(0, Math.Min(50, text.Length))}...");
                
                var requestBody = new
                {
                    input = text
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogDebug($"[MODERATION] Sending request to OpenAI...");
                var response = await _httpClient.PostAsync("moderations", content);
                
                if (!response.IsSuccessStatusCode) 
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"[MODERATION] OpenAI API failed: {response.StatusCode} - {errorBody}");
                    _logger.LogWarning("[MODERATION] Falling back to safe mode - allowing content");
                    return true; // Fail open - don't block if API fails
                }

                var responseString = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"[MODERATION] Received response: {responseString.Substring(0, Math.Min(200, responseString.Length))}...");
                
                using var doc = JsonDocument.Parse(responseString);
                
                var results = doc.RootElement.GetProperty("results");
                if (results.GetArrayLength() > 0)
                {
                    var result = results[0];
                    var categoryScores = result.GetProperty("category_scores");
                    
                    // Define stricter thresholds (higher = less sensitive)
                    const double HATE_THRESHOLD = 0.8;           
                    const double HARASSMENT_THRESHOLD = 0.8;     
                    const double THREAT_THRESHOLD = 0.8;         
                    const double VIOLENCE_THRESHOLD = 0.8;       
                    const double SEXUAL_THRESHOLD = 0.9;         
                    
                    bool shouldBlock = false;
                    var violations = new List<string>();
                    
                    // Check each category with our custom thresholds
                    if (categoryScores.TryGetProperty("hate", out var hateScore))
                    {
                        var score = hateScore.GetDouble();
                        if (score >= HATE_THRESHOLD)
                        {
                            shouldBlock = true;
                            violations.Add($"Hate ({score:F2})");
                        }
                    }
                    
                    if (categoryScores.TryGetProperty("hate/threatening", out var hateThreatScore))
                    {
                        var score = hateThreatScore.GetDouble();
                        if (score >= THREAT_THRESHOLD)
                        {
                            shouldBlock = true;
                            violations.Add($"Hate/Threatening ({score:F2})");
                        }
                    }
                    
                    if (categoryScores.TryGetProperty("harassment", out var harassmentScore))
                    {
                        var score = harassmentScore.GetDouble();
                        if (score >= HARASSMENT_THRESHOLD)
                        {
                            shouldBlock = true;
                            violations.Add($"Harassment ({score:F2})");
                        }
                    }
                    
                    if (categoryScores.TryGetProperty("harassment/threatening", out var harassThreatScore))
                    {
                        var score = harassThreatScore.GetDouble();
                        if (score >= THREAT_THRESHOLD)
                        {
                            shouldBlock = true;
                            violations.Add($"Harassment/Threatening ({score:F2})");
                        }
                    }
                    
                    if (categoryScores.TryGetProperty("violence", out var violenceScore))
                    {
                        var score = violenceScore.GetDouble();
                        if (score >= VIOLENCE_THRESHOLD)
                        {
                            shouldBlock = true;
                            violations.Add($"Violence ({score:F2})");
                        }
                    }
                    
                    if (categoryScores.TryGetProperty("violence/graphic", out var violenceGraphicScore))
                    {
                        var score = violenceGraphicScore.GetDouble();
                        if (score >= VIOLENCE_THRESHOLD)
                        {
                            shouldBlock = true;
                            violations.Add($"Violence/Graphic ({score:F2})");
                        }
                    }
                    
                    if (categoryScores.TryGetProperty("sexual", out var sexualScore))
                    {
                        var score = sexualScore.GetDouble();
                        if (score >= SEXUAL_THRESHOLD)
                        {
                            shouldBlock = true;
                            violations.Add($"Sexual ({score:F2})");
                        }
                    }
                    
                    if (shouldBlock)
                    {
                        _logger.LogWarning($"=== OPENAI MODERATION: Content blocked with custom thresholds ===");
                        _logger.LogWarning($"Text: {text.Substring(0, Math.Min(100, text.Length))}...");
                        _logger.LogWarning($"Violations: {string.Join(", ", violations)}");
                        return false;
                    }
                    
                    return true; // Content is safe
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MODERATION] Error calling OpenAI Moderation API");
                return true; // Fail open
            }
        }
    }
}
