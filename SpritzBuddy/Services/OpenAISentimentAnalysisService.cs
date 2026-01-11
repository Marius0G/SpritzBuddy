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
            IConfiguration configuration, 
            ILogger<OpenAISentimentAnalysisService> logger)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["OpenAI:ApiKey"] 
                      ?? throw new ArgumentNullException("OpenAI:ApiKey not configured");
            _logger = logger;
            
            // Configurare HttpClient pentru OpenAI API
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<SentimentResult> AnalyzeSentimentAsync(string text)
        {
            try
            {
                // CRITICAL: Enhanced multilingual system prompt - ROMANIAN IS PRIMARY
                var systemPrompt = @"YOU MUST ANALYZE ROMANIAN LANGUAGE PERFECTLY. This is CRITICAL.

You are an EXPERT sentiment analysis AI specializing in ROMANIAN and English.

RESPOND ONLY WITH JSON:
{""label"": ""positive|neutral|negative"", ""confidence"": 0.0-1.0}

=== ROMANIAN LANGUAGE ANALYSIS (MANDATORY) ===

NEGATIVE EXPRESSIONS (confidence 0.6-0.9):
- ""prost"", ""proastă"", ""prostule"", ""proști"" → negative 0.7
- ""idiot"", ""idioată"", ""tâmpit"", ""tâmpită"" → negative 0.7  
- ""ești prost"", ""ești idiot"" → negative 0.75
- ""te urăsc"", ""urât"", ""nasol"" → negative 0.8-0.9
- ""nu-mi place"", ""nu îmi place"" → negative 0.6
- ""rău"", ""groaznic"", ""oribil"" → negative 0.7
- ""te-ai săturat"", ""plictisitor"" → negative 0.6

POSITIVE EXPRESSIONS (confidence 0.7-0.9):
- ""frumos"", ""superb"", ""minunat"" → positive 0.8
- ""îmi place"", ""iubesc"", ""adorabil"" → positive 0.85
- ""grozav"", ""fantastic"", ""perfect"" → positive 0.9
- ""bun"", ""ok"", ""decent"" → positive/neutral 0.6

NEUTRAL EXPRESSIONS (confidence 0.5-0.7):
- ""bine"", ""ok"", ""mă rog"" → neutral 0.6
- ""așa și așa"", ""normal"" → neutral 0.7
- ""poate"", ""nu știu"" → neutral 0.6

=== ENGLISH LANGUAGE ANALYSIS ===
- ""stupid"", ""dumb"" → negative 0.65
- ""you are mean"" → negative 0.6
- ""hate"", ""terrible"" → negative 0.85
- ""love"", ""great"", ""amazing"" → positive 0.85
- ""ok"", ""fine"", ""meh"" → neutral 0.7

=== SCORING RULES ===
HIGH CONFIDENCE (0.8-1.0): Clear extreme emotion
MEDIUM CONFIDENCE (0.6-0.8): Clear but moderate
LOW CONFIDENCE (0.4-0.6): Ambiguous or mild

=== EXAMPLES ===
Romanian:
""ești prost"" → {""label"": ""negative"", ""confidence"": 0.75}
""super frumos"" → {""label"": ""positive"", ""confidence"": 0.85}
""e ok"" → {""label"": ""neutral"", ""confidence"": 0.6}
""te urăsc"" → {""label"": ""negative"", ""confidence"": 0.9}
""îmi place mult"" → {""label"": ""positive"", ""confidence"": 0.8}

English:
""you are stupid"" → {""label"": ""negative"", ""confidence"": 0.65}
""I love this"" → {""label"": ""positive"", ""confidence"": 0.85}

RESPOND ONLY WITH JSON. NO EXPLANATIONS.";

                var userPrompt = $"Analyze sentiment: \"{text}\"";

                var requestBody = new
                {
                    model = ModelName,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    temperature = 0.1, // Lower for more consistent results
                    max_tokens = 50,
                    response_format = new { type = "json_object" }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("[SENTIMENT] Analyzing: {Text}", text);

                var response = await _httpClient.PostAsync("chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[SENTIMENT] API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new SentimentResult
                    {
                        Success = false,
                        ErrorMessage = $"API Error: {response.StatusCode}"
                    };
                }

                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                var assistantMessage = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                if (string.IsNullOrEmpty(assistantMessage))
                {
                    _logger.LogError("[SENTIMENT] Empty response from API");
                    return new SentimentResult
                    {
                        Success = false,
                        ErrorMessage = "Empty response"
                    };
                }

                _logger.LogInformation("[SENTIMENT] AI Response: {Response}", assistantMessage);

                var sentimentData = JsonSerializer.Deserialize<SentimentResponse>(assistantMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (sentimentData == null)
                {
                    _logger.LogError("[SENTIMENT] Failed to parse response: {Response}", assistantMessage);
                    return new SentimentResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to parse"
                    };
                }

                var label = sentimentData.Label?.ToLower() switch
                {
                    "positive" => "positive",
                    "negative" => "negative",
                    _ => "neutral"
                };

                var confidence = Math.Clamp(sentimentData.Confidence, 0.0, 1.0);

                _logger.LogInformation("[SENTIMENT] ✓ Result: {Label} ({Confidence:P0}) for: {Text}", label, confidence, text);

                return new SentimentResult
                {
                    Label = label,
                    Confidence = confidence,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SENTIMENT] Exception analyzing: {Text}", text);
                return new SentimentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
