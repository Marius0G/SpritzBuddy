using System.Threading.Tasks;

namespace SpritzBuddy.Services
{
    // Clasa pentru rezultatul analizei de sentiment
    public class SentimentResult
    {
        public string Label { get; set; } = "neutral"; // positive, neutral, negative
        public double Confidence { get; set; } = 0.0; // 0.0 - 1.0
        public bool Success { get; set; } = false;
        public string? ErrorMessage { get; set; }
    }

    public interface ISentimentAnalysisService
    {
        Task<SentimentResult> AnalyzeSentimentAsync(string text);
    }
}
