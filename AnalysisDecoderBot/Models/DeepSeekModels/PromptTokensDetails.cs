using System.Text.Json.Serialization;

namespace AnalysisDecoderBot.Models.DeepSeekModels
{
    public class PromptTokensDetails
    {
        [JsonPropertyName("cached_tokens")]
        public int CachedTokens { get; set; }
    }
}
