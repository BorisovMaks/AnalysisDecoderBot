using System.Text.Json.Serialization;

namespace AnalysisDecoderBot.Models.DeepSeekModels
{
    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}
