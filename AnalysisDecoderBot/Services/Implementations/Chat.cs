using AnalysisDecoderBot.Helpers;
using AnalysisDecoderBot.Models;
using AnalysisDecoderBot.Resources;
using AnalysisDecoderBot.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using UglyToad.PdfPig;

namespace AnalysisDecoderBot.Services.Implementations
{
    public class Chat : IChat, IDisposable
    {
        private readonly ILogger _logger;
        private readonly HttpClient _client;

        private readonly List<string> _supportedFilesFormat = [".pdf"];
        private readonly string _deepSeekApiUrl = "https://api.deepseek.com/v1/chat/completions";
        private readonly string _chatModel = "deepseek-chat";
        private readonly string _mediaType = "application/json";
        private int _spentTokens = 0;
        private bool _isRun;
        private readonly Stopwatch _stopwatchText = Stopwatch.StartNew();
        private readonly Stopwatch _stopwatchFile = Stopwatch.StartNew();

        public bool IsRunning() => _isRun;

        public Chat(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            var deepSeekApiKey = configuration.GetValue<string>("DeepSeekAccessAPI");

            if (deepSeekApiKey == null)
            {
                var ex = new Exception("Не задан токен доступа к DeepSeek");
                _logger.LogError(nameof(Chat), ex);
                throw ex;
            }

            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", deepSeekApiKey);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(_mediaType));
            _stopwatchText.Start();
            _stopwatchFile.Start();
        }

        public async Task<string> SendTextMessageAsync(string message)
        {
            if (!_isRun)
            {
                return string.Empty;
            }

            _stopwatchText.Restart();
            _logger.LogInfo($"Начало обработки запроса к AI {_stopwatchText.ElapsedMilliseconds}");

            // Тело запроса в формате JSON (зависит от API)
            var requestBody = new
            {
                model = _chatModel,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = message
                    }
                }
            };

            // Сериализуем тело запроса в JSON
            string jsonBody = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, _mediaType);

            // Отправляем POST-запрос
            HttpResponseMessage response = await _client.PostAsync(_deepSeekApiUrl, content);

            // Читаем ответ
            string responseBody = await response.Content.ReadAsStringAsync();
            var responseModel = ParseResponseHelper<DeepSeekResponse>.ParseResponse(responseBody);

            if (responseModel == null)
            {
                var ex = new Exception("Не удалось распарсить ответ от AI");
                _logger.LogError(nameof(SendTextMessageAsync), ex);
                throw ex; 
            }

            _spentTokens += responseModel.Usage.TotalTokens;
            _logger.LogInfo($"За сообщение '{responseModel.Usage.TotalTokens}' токенов. Общее '{_spentTokens}' токенов");

            _logger.LogInfo($"Окончание обработки {_stopwatchText.Elapsed}");
            _logger.LogInfo("Запрос отправлен");
            _stopwatchText.Stop();
            
            return CreateMessage(responseModel);
        }

        public async Task<MedicalReportResponse> SendFileMessageAsync(string path, UserModel user)
        {
            MedicalReportResponse output = new();
            if (!_isRun)
            {
                return null;
            }

            if (!File.Exists(path))
            {
                return null;
            }

            _stopwatchFile.Restart();
            string textFromPdf = string.Empty;

            try
            {
                textFromPdf = ParsePDFFile(path, user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка обработки файла", ex);
            }

            try
            {
                var requestBody = new
                {
                    model = "deepseek-chat",
                    messages = new[]
                    {
                        new 
                        { 
                            role = "user", 
                            content = textFromPdf 
                        }
                    }
                };

                string jsonBody = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync(_deepSeekApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseJson = await response.Content.ReadAsStringAsync();
                    var apiResponse = ParseResponseHelper<DeepSeekResponse>.ParseResponse(responseJson);

                    if (apiResponse == null)
                    {
                        var apiNullerror = $"Не удалось распарсить ответ от AI. Закончено в '{_stopwatchFile.Elapsed}'";
                        _logger.LogWarning(apiNullerror);
                        output.Error = apiNullerror;

                        _stopwatchFile.Stop();
                        return output;
                    }

                    string deepSeekResponseText = apiResponse.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;

                    var error = "Файл отклонён.";
                    if (deepSeekResponseText == error)
                    {
                        output.Error = error;
                        _spentTokens += apiResponse.Usage.TotalTokens;

                        _logger.LogInfo($"За сообщение '{apiResponse.Usage.TotalTokens}' токенов. Общее '{_spentTokens}' токенов. Закончено в '{_stopwatchFile.Elapsed}'");
                        _logger.LogWarning(error);

                        _stopwatchFile.Stop();

                        return output;
                    }

                    ParseResponse(deepSeekResponseText, output);

                    _spentTokens += apiResponse.Usage.TotalTokens;
                    _logger.LogInfo($"За сообщение '{apiResponse.Usage.TotalTokens}' токенов. Общее '{_spentTokens}' токенов. Закончено в '{_stopwatchFile.Elapsed}'");

                    _stopwatchFile.Stop();
                    return output;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Ошибка AI API: {response.StatusCode}, Response: {errorResponse}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при вызове API. Закончено в '{_stopwatchFile.Elapsed}'", ex);
                _stopwatchFile.Stop();
            }

            return null;
        }

        private static string ParsePDFFile(string path, UserModel user)
        {
            string textFromPdf;
            var sb = new StringBuilder();
            sb.AppendLine(user.ToString());
            sb.AppendLine(Strings.FirstLineStringInRequest);

            using (var pdfStream = File.OpenRead(path))
            using (PdfDocument document = PdfDocument.Open(path))
            {
                foreach (var page in document.GetPages())
                {
                    sb.AppendLine(page.Text);
                }

                textFromPdf = sb.ToString();
            }

            return textFromPdf;
        }

        private static void ParseResponse(string deepSeekResponseText, MedicalReportResponse output)
        {
            string recomendationsStart = "<Recommendations>";
            string recomendationsEnd = "</Recommendations>";

            string indicatorsStart = "<Indicators>";
            string indicatorsEnd = "</Indicators>";

            string infoStart = "<Info>";
            string infoEnd = "</Info>";

            int recomendationsStartIndex = deepSeekResponseText.IndexOf(recomendationsStart) + recomendationsStart.Length;
            int recomendationsEndIndex = deepSeekResponseText.IndexOf(recomendationsEnd);

            int indicatorsStartIndex = deepSeekResponseText.IndexOf(indicatorsStart) + indicatorsStart.Length;
            int indicatorsEndIndex = deepSeekResponseText.IndexOf(indicatorsEnd);

            int infoStartIndex = deepSeekResponseText.IndexOf(infoStart) + infoStart.Length;
            int infoEndIndex = deepSeekResponseText.IndexOf(infoEnd);

            output.Recommendations = deepSeekResponseText[recomendationsStartIndex..recomendationsEndIndex];
            output.Indicators = deepSeekResponseText[indicatorsStartIndex..indicatorsEndIndex];
            output.Info = deepSeekResponseText[infoStartIndex..infoEndIndex];
        }

        private static string CreateMessage(DeepSeekResponse responseModel)
        {
            StringBuilder sb = new();

            foreach (var chois in responseModel.Choices)
            {
                sb.AppendLine(chois.Message.Content);
            }

            return sb.ToString();
        }

        public void Start()
        {
            _isRun = true;
            _logger.LogInfo($"'{nameof(Chat)} {nameof(Start)}' Запущен AI");
        }

        public void Stop()
        {
            _isRun = false;
            _logger.LogInfo($"'{nameof(Chat)} {nameof(Stop)}' Остановлен AI");
        }

        public void Dispose()
        {
            _client.Dispose();
            _stopwatchText.Stop();
            _stopwatchFile.Stop();
        }

        public IEnumerable<string> GetSupportedFilesFormat()
        {
            return _supportedFilesFormat;
        }

        public int GetStatistics()
        {
            return _spentTokens;
        }
    }
}
