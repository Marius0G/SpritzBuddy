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
                // Enhanced multilingual system prompt with explicit Romanian examples
                var systemPrompt = @"You are an expert multilingual sentiment analysis AI that MUST work perfectly for Romanian language.

CRITICAL: You MUST analyze Romanian text correctly. Romanian is your PRIMARY language.

Analyze the sentiment and respond ONLY with this JSON:
{""label"": ""positive|neutral|negative"", ""confidence"": 0.0-1.0}

ROMANIAN LANGUAGE RULES (VERY IMPORTANT):
- ""prost"", ""proastă"", ""prostule"" → negative (0.7) - common Romanian insult
- ""idiot"", ""tâmpit"" → negative (0.7) - Romanian insults
- ""urât"", ""nasol"" → negative (0.6) - mild negative
- ""frumos"", ""super"", ""grozav"" → positive (0.8) - Romanian positive
- ""bine"", ""ok"", ""decent"" → neutral (0.6) - Romanian neutral
- ""îmi place"" → positive (0.7) - I like it
- ""nu-mi place"" → negative (0.6) - I don't like it
- ""te urăsc"" → negative (0.9) - I hate you (strong)
- ""ești minunat"" → positive (0.9) - you are wonderful

ENGLISH LANGUAGE RULES:
- ""stupid"", ""dumb"" → negative (0.6-0.7) - mild insults
- ""you are mean"" → negative (0.6) - mild criticism
- ""hate you"" → negative (0.9) - strong negative
- ""love this"" → positive (0.9) - strong positive
- ""meh"", ""okay"" → neutral (0.7)

CONFIDENCE SCORING:
- 0.9-1.0: Very certain (extreme language)
- 0.7-0.9: Quite certain (clear sentiment)
- 0.5-0.7: Moderate (mixed or mild)
- 0.3-0.5: Uncertain (ambiguous)

IMPORTANT: 
- Context matters
- Cultural expressions are understood
- Mild criticism is negative but low confidence
- Only extreme/aggressive language gets high confidence negative

Romanian Examples:
{""label"": ""negative"", ""confidence"": 0.7} for ""ești prost""
{""label"": ""positive"", ""confidence"": 0.8} for ""super frumos""
{""label"": ""neutral"", ""confidence"": 0.6} for ""e ok""
{""label"": ""negative"", ""confidence"": 0.9} for ""te urăsc enorm""

Respond ONLY with JSON. No explanations.";

                var userPrompt = $"Analyze: \"{text}\"";

                var requestBody = new
                {
                    model = ModelName,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    temperature = 0.2, // Lower for more consistent Romanian handling
                    max_tokens = 100,
                    response_format = new { type = "json_object" }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("[SENTIMENT] Analyzing text: {Text}", text.Substring(0, Math.Min(50, text.Length)));

                var response = await _httpClient.PostAsync("chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[SENTIMENT] OpenAI API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
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
                    return new SentimentResult
                    {
                        Success = false,
                        ErrorMessage = "Empty response from API"
                    };
                }

                _logger.LogInformation("[SENTIMENT] AI Response: {Response}", assistantMessage);

                var sentimentData = JsonSerializer.Deserialize<SentimentResponse>(assistantMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (sentimentData == null)
                {
                    return new SentimentResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to parse sentiment response"
                    };
                }

                var label = sentimentData.Label?.ToLower() switch
                {
                    "positive" => "positive",
                    "negative" => "negative",
                    _ => "neutral"
                };

                var confidence = Math.Clamp(sentimentData.Confidence, 0.0, 1.0);

                _logger.LogInformation("[SENTIMENT] Result: {Label} ({Confidence:P0}) for text: {Text}", label, confidence, text);

                return new SentimentResult
                {
                    Label = label,
                    Confidence = confidence,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SENTIMENT] Error analyzing sentiment for text: {Text}", text);
                return new SentimentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
