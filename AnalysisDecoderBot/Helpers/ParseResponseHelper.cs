using System.Text.Json;

namespace AnalysisDecoderBot.Helpers
{
    public static class ParseResponseHelper<T>
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public static T ParseResponse(string response)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(response, _options);
            }
            catch (Exception)
            {
                throw new Exception("Ошибка десериализации файла");
            }
        }
    }
}
