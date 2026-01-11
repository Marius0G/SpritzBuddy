using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SpritzBuddy.Services
{
    // Internal class for deserializing OpenAI response
    internal class SentimentResponse
    {
        [JsonPropertyName("label")]
        public string? Label { get; set; }
        
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }
    
    public class OpenAISentimentAnalysisService : ISentimentAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<OpenAISentimentAnalysisService> _logger;

        private const string BaseUrl = "https://api.openai.com/v1/";
        private const string ModelName = "gpt-4o-mini";

        public OpenAISentimentAnalysisService(
            HttpClient httpClient,
            IConfiguration configuration, 
            ILogger<OpenAISentimentAnalysisService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"] 
                      ?? throw new ArgumentNullException("OpenAI:ApiKey nu este configurat în appsettings.json");
            _logger = logger;
            
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<SentimentResult> AnalyzeSentimentAsync(string text)
        {
            try
            {
                var systemPrompt = "You are a sentiment analysis assistant. Analyze the sentiment of the given text and respond ONLY with a JSON object in this exact format:\n" +
                    "{\"label\": \"positive|neutral|negative\", \"confidence\": 0.0-1.0}\n" +
                    "Rules:\n" +
                    "- label must be exactly one of: positive, neutral, negative\n" +
                    "- confidence must be a number between 0.0 and 1.0\n" +
                    "- Do not include any other text, only the JSON object";

                var userPrompt = $"Analyze the sentiment of this comment: \"{text}\"";

                var requestBody = new
                {
                    model = ModelName,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    temperature = 0.1,
                    max_tokens = 50,
                    response_format = new { type = "json_object" }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Trimitem cererea de analiză sentiment către OpenAI API");

                var response = await _httpClient.PostAsync("chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Eroare OpenAI API: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new SentimentResult
                    {
                        Success = false,
                        ErrorMessage = $"Eroare API: {response.StatusCode}"
                    };
                }

                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                var assistantMessage = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                if (string.IsNullOrEmpty(assistantMessage))
                {
                    return new SentimentResult
                    {
                        Success = false,
                        ErrorMessage = "Răspuns gol de la API"
                    };
                }

                var sentimentData = JsonSerializer.Deserialize<SentimentResponse>(assistantMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (sentimentData == null)
                {
                    return new SentimentResult
                    {
                        Success = false,
                        ErrorMessage = "Nu s-a putut parsa răspunsul sentiment"
                    };
                }

                var label = sentimentData.Label?.ToLower() switch
                {
                    "positive" => "positive",
                    "negative" => "negative",
                    _ => "neutral"
                };

                var confidence = Math.Clamp(sentimentData.Confidence, 0.0, 1.0);

                return new SentimentResult
                {
                    Label = label,
                    Confidence = confidence,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare la analiza sentimentului");
                return new SentimentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
