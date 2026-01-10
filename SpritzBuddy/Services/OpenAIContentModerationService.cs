using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace SpritzBuddy.Services
{
    public class OpenAIContentModerationService : IContentModerationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<OpenAIContentModerationService> _logger;

        public OpenAIContentModerationService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAIContentModerationService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
            _logger = logger;
            
            _logger.LogInformation($"OpenAI Content Moderation initialized. API Key configured: {!string.IsNullOrEmpty(_apiKey)}");
        }

        public async Task<bool> IsContentSafeAsync(string text)
        {
            // If no API key is configured or text is empty
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogDebug("Empty text provided for moderation - allowing");
                return true;
            }
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("OpenAI API Key is missing. Cannot perform content moderation.");
                return false; // Fail closed for safety
            }

            try 
            {
                _logger.LogDebug($"Checking content for moderation: {text.Substring(0, Math.Min(50, text.Length))}...");
                
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
                    _logger.LogError($"OpenAI Moderation API failed: {response.StatusCode} - {errorBody}");
                    return false; // Fail closed for safety
                }

                var responseString = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"OpenAI API Response: {responseString}");
                
                using var doc = JsonDocument.Parse(responseString);
                
                // OpenAI returns "flagged": true if inappropriate
                var results = doc.RootElement.GetProperty("results");
                if (results.GetArrayLength() > 0)
                {
                    var flagged = results[0].GetProperty("flagged").GetBoolean();
                    
                    if (flagged)
                    {
                        _logger.LogWarning($"Content flagged as inappropriate: {text.Substring(0, Math.Min(50, text.Length))}...");
                        
                        // Log which categories were flagged
                        var categories = results[0].GetProperty("categories");
                        var flaggedCategories = new List<string>();
                        
                        foreach (var category in categories.EnumerateObject())
                        {
                            if (category.Value.GetBoolean())
                            {
                                flaggedCategories.Add(category.Name);
                            }
                        }
                        
                        _logger.LogWarning($"Flagged categories: {string.Join(", ", flaggedCategories)}");
                    }
                    else
                    {
                        _logger.LogDebug("Content is safe");
                    }
                    
                    return !flagged; // Return true if NOT flagged (safe)
                }
                
                _logger.LogWarning("OpenAI API returned unexpected response format");
                return false; // Fail closed
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling OpenAI Moderation API");
                return false; // Fail closed
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing OpenAI API response");
                return false; // Fail closed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling OpenAI Moderation API");
                return false; // Fail closed
            }
        }
    }
}
