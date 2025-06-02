using AnalysisDecoderBot.Enums;
using AnalysisDecoderBot.Models;
using AnalysisDecoderBot.Models.DeepSeekModels;
using AnalysisDecoderBot.Resources;
using AnalysisDecoderBot.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Reflection;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnalysisDecoderBot.Services.Implementations
{
    public class TelegramMessenger : ITelegramMessenger
    {
        private readonly ISqLiteRepository _repository;
        private readonly ITelegramBotClient _client;
        private readonly CancellationTokenSource _cts = new();
        private readonly IChat _chat;
        private readonly ILogger _logger;
        private readonly ITelegramMenuService _telegramMenuService;
        private readonly IUserStatusService _userStatusService;

        private long _awaitUserDataChatId = default;

        private readonly ReceiverOptions _options = new()
        {
            AllowedUpdates =
            [
                UpdateType.Message,
                UpdateType.CallbackQuery
            ],
            DropPendingUpdates = true,
        };

        public TelegramMessenger(
            ILogger logger,
            ISqLiteRepository repository,
            IConfiguration configuration,
            IUserStatusService userStatusService)
        {
            _logger = logger;
            _repository = repository;
            _userStatusService = userStatusService;

            _telegramMenuService = new TelegramMenuService();

            var telegramToken = configuration.GetValue<string>("TelegramAccessAPI");

            if (telegramToken == null)
            {
                var ex = new Exception("Не задан токен доступа к Telegram");
                _logger.LogError(nameof(TelegramMessenger), ex);
                throw ex;
            }

            _client = new TelegramBotClient(telegramToken);
            _chat = new Chat(_logger, configuration);
            _chat.Start();
        }

        #region Start|Stop

        public async Task StartAsync()
        {
            _client.StartReceiving(
                HandleUpdateAsync,
                HandlePollingErrorAsync,
                receiverOptions: _options,
                cancellationToken: _cts.Token
            );

            var me = await _client.GetMe();
            _logger.LogInfo($"Бот @{me.Username} запущен!");
        }

        public async Task StopAsync()
        {
            await _cts.CancelAsync();
            _chat.Dispose();
        }


        #endregion

        #region Message Handlers

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            try
            {
                var response = update.Message != null ? update.Message.Text : update.CallbackQuery.Message.Text;
                var responseMessage = update.Message ?? update.CallbackQuery.Message;
                var chatId = update.Message != null ? update.Message.Chat.Id : update.CallbackQuery.Message.Chat.Id;

                if (_awaitUserDataChatId == chatId)
                {
                    _awaitUserDataChatId = default;
                }

                var activeUserData = _userStatusService.GetActiveUserData(chatId);

                if (activeUserData != null)
                {
                    var user = await _repository.GetUserByChatIdAsync(chatId);

                    switch (activeUserData.WorkMode)
                    {
                        case TelegramWorkModeEnum.Temperature:
                            await Temperature(bot, responseMessage, user, activeUserData, response, ct);
                            break;
                        case TelegramWorkModeEnum.Pressure:
                            await Pressure(bot, responseMessage, user, activeUserData, response, ct);
                            break;
                        case TelegramWorkModeEnum.Name:
                            await Name(bot, responseMessage, ct);
                            break;
                        case TelegramWorkModeEnum.Gender:
                            await Gender(bot, responseMessage, ct);
                            break;
                        case TelegramWorkModeEnum.Age:
                            await Age(bot, responseMessage, ct);
                            break;
                        case TelegramWorkModeEnum.Height:
                            await Height(bot, responseMessage, ct);
                            break;
                        case TelegramWorkModeEnum.Weight:
                            await Weight(bot, responseMessage, ct);
                            break;
                    }
                }

                switch (update)
                {
                    case { Message: { } message }:

                        await Task.Run(async () =>
                        {
                            await HandleMessageAsync(bot, message, ct);
                        }, ct);

                        break;

                    case { CallbackQuery: { } callbackQuery }:

                        await Task.Run(async () =>
                        {
                            await HandleCallbackQueryAsync(bot, callbackQuery, ct);
                        }, ct);

                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(HandleUpdateAsync), ex);
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            _logger.LogError(nameof(HandlePollingErrorAsync), ex);
            return Task.CompletedTask;
        }

        private async Task HandleMessageAsync(ITelegramBotClient bot, Telegram.Bot.Types.Message message, CancellationToken ct)
        {
            _logger.LogInfo($"Получено сообщение от {message.From?.Username}: {message.Text}");

            var user = await _repository.GetUserByChatIdAsync(message.Chat.Id);

            if (message.Text is { } text && text.Split(' ')[0] == BotCommands.Start && message.Document == null)
            {
                await SendStartMenu(bot, message, user, ct);
                return;
            }

            if (user != null)
            {
                if (message.Document == null)
                {
                    return;
                }

                if (!CheckFile(message))
                {
                    return;
                }

                await bot.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Файл проверен и успешно загружен\nОперация может занять некоторое время, ожидайте ответа",
                    cancellationToken: ct);
                try
                {
                    var file = await _client.GetFile(message.Document.FileId, cancellationToken: ct);
                    var filePath = file.FilePath;

                    string tempFilePath = Path.Combine(Path.GetTempPath(), file.FileId + Path.GetExtension(filePath));

                    await using (var saveFileStream = File.OpenWrite(tempFilePath))
                    {
                        await _client.DownloadFile(filePath, saveFileStream, ct);
                    }

                    var medicalReportResponse = await _chat.SendFileMessageAsync(tempFilePath, user);

                    if (!string.IsNullOrEmpty(medicalReportResponse.Error))
                    {
                        await _client.SendMessage(chatId: message.Chat.Id, text: medicalReportResponse.Error, cancellationToken: ct);

                        await SendStartMenu(bot, message, user, ct);
                        return;
                    }

                    await _client.SendMessage(chatId: message.Chat.Id, text: medicalReportResponse.Recommendations, cancellationToken: ct);

                    _ = await _repository.AddAnalysisAsync(new Analysis(AnalysisTypeEnum.Default)
                    {
                        Recommendations = medicalReportResponse.Recommendations,
                        DateTime = DateTime.Now,
                        UserId = user.Id,
                        Indicators = medicalReportResponse.Indicators,
                        Info = medicalReportResponse.Info,
                    });

                    await SendStartMenu(bot, message, user, ct);
                }
                catch (Exception ex)
                {
                    var errorMessage = "Ошибка при обработке документа";

                    await _client.SendMessage(chatId: message.Chat.Id, text: $"{errorMessage}: {ex.Message}", cancellationToken: ct);

                    _logger.LogError(errorMessage, ex);
                }
            }
            else
            {
                return;
            }
        }

        private async Task HandleCallbackQueryAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
        {
            _logger.LogInfo($"Обработка callback от {callbackQuery.From.Username}: {callbackQuery.Data}");

            var user = await _repository.GetUserByChatIdAsync(callbackQuery.Message.Chat.Id);

            var activeUserData = _userStatusService.GetActiveUserData(callbackQuery.Message!.Chat.Id);
            if (activeUserData == null)
            {
                _logger.LogWarning($"Пользователь с Id чата'{callbackQuery.Message!.Chat.Id}' не активен");
                return;
            }

            await bot.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: $"Вы выбрали: {callbackQuery.Data}",
                cancellationToken: ct);

            switch (callbackQuery.Data)
            {
                case BotCommands.Contacts:
                    {
                        await Contacts(bot, callbackQuery, user, ct);
                        break;
                    }
                case BotCommands.About:
                    {
                        await About(bot, callbackQuery, user, ct);
                        break;
                    }
                case BotCommands.Back:
                    {
                        await Back(bot, callbackQuery, user, activeUserData, ct);
                        break;
                    }
                case BotCommands.Analysis:
                    {
                        await Analysis(bot, callbackQuery, ct);
                        break;
                    }
                case BotCommands.History:
                    {
                        await History(bot, callbackQuery, ct);
                        break;
                    }

                case BotCommands.AnalysisHistory:
                    {
                        await AnalysisHistory(bot, callbackQuery, user, ct);
                        break;
                    }

                case BotCommands.TemperatureHistory:
                    {
                        await TemperatureHistory(bot, callbackQuery, user, ct);
                        break;
                    }

                case BotCommands.PressureHistory:
                    {
                        await PressureHistory(bot, callbackQuery, user, ct);
                        break;
                    }

                case BotCommands.PDF:
                    {
                        await PDF(bot, callbackQuery, ct);
                        break;
                    }

                case BotCommands.Manual:
                    {
                        await Manual(bot, callbackQuery, user, ct);
                        break;
                    }
                case BotCommands.OnOff:
                    {
                        await OnOff(bot, callbackQuery, user, activeUserData, ct);
                        break;
                    }
                case BotCommands.Temperature:
                    {
                        await Temperature(bot, callbackQuery, activeUserData, ct);
                        break;
                    }
                case BotCommands.Pressure:
                    {
                        await Pressure(bot, callbackQuery, activeUserData, ct);
                        break;
                    }

                case BotCommands.Statistics:
                    {
                        await Statistics(bot, callbackQuery, user, ct);
                        break;
                    }

                case BotCommands.Exit:
                    {
                        await Exit(bot, callbackQuery, user, ct);
                        break;
                    }

                case BotCommands.Cancel:
                    {
                        await SendStartMenu(bot, callbackQuery.Message, user, ct);
                        break;
                    }

                case BotCommands.Ok:
                    {
                        await Ok(bot, callbackQuery, user, ct);
                        break;
                    }

                case BotCommands.SignUp:
                    {
                        await bot.SendMessage(
                            chatId: callbackQuery.Message!.Chat.Id,
                            text: "Введите Ваше имя",
                            cancellationToken: ct);

                        activeUserData.WorkMode = TelegramWorkModeEnum.Name;
                        _awaitUserDataChatId = callbackQuery.Message!.Chat.Id;

                        break;
                    }

                case BotCommands.Name:
                    {

                        break;
                    }
                case BotCommands.Gender:
                    {

                        break;
                    }
                case BotCommands.Age:
                    {

                        break;
                    }
                case BotCommands.Height:
                    {

                        break;
                    }
                case BotCommands.Weight:
                    {

                        break;
                    }
                default:
                    {
                        await Default(bot, callbackQuery, ct);
                        break;
                    }
            }
        }

        #endregion

        #region Methods

        private static async Task PDF(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
        {
            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: "Загрузите pdf файл с анализами",
                cancellationToken: ct);
        }

        private async Task Manual(ITelegramBotClient bot, CallbackQuery callbackQuery, UserModel user, CancellationToken ct)
        {
            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: "Открыто меню ввода результатов в ручную",
                cancellationToken: ct);

            await SendManualMenu(bot, callbackQuery.Message, user, ct);
        }

        private async Task Default(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
        {
            var message = $"'{callbackQuery.Data}' - Нет такой команды";

            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: message,
                cancellationToken: ct);

            _logger.LogInfo(message);
        }

        private async Task<bool> Ok(ITelegramBotClient bot, CallbackQuery callbackQuery, UserModel user, CancellationToken ct)
        {
            if (!_userStatusService.TryRemoveActiveUser(user.ChatId))
            {
                _logger.LogWarning("Не удалось удалить активного пользователя из системы");
                return false;
            }

            var allAnalysis = await _repository.GetAllUserAnalysesAsync(user.Id);
            await _repository.DeleteUserAsync(user.Id);

            foreach (var analysis in allAnalysis)
            {
                await _repository.DeleteAnalysisAsync(analysis.Id);
            }

            user = null;

            await SendStartMenu(bot, callbackQuery.Message, user, ct);
            return true;
        }

        private async Task Exit(ITelegramBotClient bot, CallbackQuery callbackQuery, UserModel user, CancellationToken ct)
        {
            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: "Подтверждение выхода из системы (с удалением всех данных)",
                cancellationToken: ct);

            await SendExitMenu(bot, callbackQuery.Message, user, ct);
        }

        private async Task Statistics(ITelegramBotClient bot, CallbackQuery callbackQuery, UserModel user, CancellationToken ct)
        {
            var allUsers = await _repository.GetAllUsersAsync();
            var allTokens = _chat.GetStatistics();
            var allActiveUsersCount = _userStatusService.GetStatistics();

            StringBuilder sb = new();
            sb.AppendLine($" '{DateTime.Now}':");
            sb.AppendLine($"Зарегистрировано пользователей - '{allUsers.Count()}'");
            sb.AppendLine($"Активно пользователей - '{allActiveUsersCount}'");
            sb.AppendLine($"Затрачено токенов с момента запуска бота - '{allTokens}'");

            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: sb.ToString(),
                cancellationToken: ct);

            await SendStartMenu(bot, callbackQuery.Message, user, ct);
        }

        private async Task Pressure(ITelegramBotClient bot, CallbackQuery callbackQuery, ActiveUserModel activeUserData, CancellationToken ct)
        {
            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: "Введите показатель артериального давления через ',' (верхнее нижнее пульс)",
                cancellationToken: ct);

            activeUserData.WorkMode = TelegramWorkModeEnum.Pressure;
            _awaitUserDataChatId = callbackQuery.Message!.Chat.Id;
        }

        private async Task Temperature(ITelegramBotClient bot, CallbackQuery callbackQuery, ActiveUserModel activeUserData, CancellationToken ct)
        {
            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: "Введите показатель температуры тела",
                cancellationToken: ct);

            activeUserData.WorkMode = TelegramWorkModeEnum.Temperature;
            _awaitUserDataChatId = callbackQuery.Message!.Chat.Id;
        }

        private async Task OnOff(ITelegramBotClient bot, CallbackQuery callbackQuery, UserModel user, ActiveUserModel activeUserData, CancellationToken ct)
        {
            activeUserData.Menu.Preview = null;
            activeUserData.Menu.IsBack = true;

            if (_chat.IsRunning())
            {
                _chat.Stop();
            }
            else
            {
                _chat.Start();
            }

            var message = _chat.IsRunning() ? "AI запущен" : "AI остановлен";
            _logger.LogWarning(message);

            await SendStartMenu(bot, callbackQuery.Message, user, ct);
        }

        private async Task PressureHistory(ITelegramBotClient bot, CallbackQuery callbackQuery, UserModel user, CancellationToken ct)
        {
            var analyses = await _repository.GetUserAnalysisAsync(user.Id, AnalysisTypeEnum.Pressure);

            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: "История анализов. Будут показаны последние 3 анализа",
                cancellationToken: ct);

            foreach (var analysis in analyses)
            {
                await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: analysis.ToString(),
                cancellationToken: ct);
            }

            await SendStartMenu(bot, callbackQuery.Message, user, ct);
        }

        private async Task TemperatureHistory(ITelegramBotClient bot, CallbackQuery callbackQuery, UserModel user, CancellationToken ct)
        {
            var analyses = await _repository.GetUserAnalysisAsync(user.Id, AnalysisTypeEnum.Temperature);

            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: "История анализов. Будут показаны последние 3 анализа",
                cancellationToken: ct);

            foreach (var analysis in analyses)
            {
                await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: analysis.ToString(),
                cancellationToken: ct);
            }

            await SendStartMenu(bot, callbackQuery.Message, user, ct);
        }

        private async Task AnalysisHistory(ITelegramBotClient bot, CallbackQuery callbackQuery, UserModel user, CancellationToken ct)
        {
            var analyses = await _repository.GetUserAnalysisAsync(user.Id, AnalysisTypeEnum.Default);

            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: "История анализов. Будут показаны последние 3 анализа",
                cancellationToken: ct);

            foreach (var analysis in analyses)
            {
                await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: analysis.ToString(),
                cancellationToken: ct);
            }

            await SendStartMenu(bot, callbackQuery.Message, user, ct);
        }

        private async Task History(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
        {
            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: "Открыто меню истории анализов",
                cancellationToken: ct);

            await SendAnalysesHistoryMenu(bot, callbackQuery.Message, ct);
        }

        private async Task Analysis(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
        {
            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: "Открыто меню анализов",
                cancellationToken: ct);

            await SendAnalysesMenu(bot, callbackQuery.Message, ct);
        }

        private async Task Back(ITelegramBotClient bot, CallbackQuery callbackQuery, UserModel user, ActiveUserModel activeUserData, CancellationToken ct)
        {
            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: "Переход назад",
                cancellationToken: ct);

            activeUserData.Menu.Preview = null;
            activeUserData.Menu.IsBack = true;

            await SendStartMenu(bot, callbackQuery.Message, user, ct);
        }

        private async Task About(ITelegramBotClient bot, CallbackQuery callbackQuery, UserModel user, CancellationToken ct)
        {
            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: Strings.AboutProgram,
                cancellationToken: ct);

            await SendStartMenu(bot, callbackQuery.Message, user, ct);
        }

        private async Task Contacts(ITelegramBotClient bot, CallbackQuery callbackQuery, UserModel user, CancellationToken ct)
        {
            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat.Id,
                text: "Для связи с администратором используйте телеграм - '@BrunoPewPew'",
                cancellationToken: ct);

            await SendStartMenu(bot, callbackQuery.Message, user, ct);
        }
        private bool CheckFile(Telegram.Bot.Types.Message message)
        {
            var extension = Path.GetExtension(message.Document.FileName);

            if (string.IsNullOrEmpty(extension) ||
                string.IsNullOrWhiteSpace(extension))
            {
                _logger.LogWarning($"Попытка загрузить неподдерживаемый файл - '{extension}'");
                return false;
            }

            if (!_chat.GetSupportedFilesFormat().Contains(extension))
            {
                _logger.LogWarning($"Попытка загрузить неподдерживаемый файл - '{extension}'");
                return false;
            }

            return true;
        }

        private async Task<bool> Pressure(
            ITelegramBotClient bot,
            Telegram.Bot.Types.Message message,
            UserModel user,
            ActiveUserModel activeUserData,
            string response,
            CancellationToken ct)
        {
            int[] indicators = [0, 0, 0];
            char[] delimiter = [','];
            string[] substrings = response.Split(delimiter);

            if (substrings.Length > 3)
            {
                _logger.LogInfo("Не удалось прочитать введённые значения");
                return false;
            }

            for (int i = 0; i < substrings.Length; i++)
            {
                if (int.TryParse(substrings[i], out var result))
                {
                    indicators[i] = result;
                }
            }

            StringBuilder sb = new();

            for (int i = 0; i < indicators.Length; i++)
            {
                string type = string.Empty;

                if (i == 0)
                {
                    type = "Верхнее";
                }
                else if (i == 1)
                {
                    type = "Нижнее";
                }
                else if (i == 2)
                {
                    type = "Пульс";
                }
                else
                {
                    _logger.LogInfo("Не удалось прочитать введённые значения");
                    return false;
                }

                sb.AppendLine($"{type} - {indicators[i]}");
            }

            var analis = new Analysis(AnalysisTypeEnum.Pressure)
            {
                DateTime = DateTime.Now,
                UserId = user.Id,
                Info = AnalysisTypeEnum.Pressure.GetDescription(),
                Indicators = sb.ToString(),
                Recommendations = string.Empty
            };

            _ = await _repository.AddAnalysisAsync(analis);

            await SendStartMenu(bot, message, user, ct);
            activeUserData.WorkMode = TelegramWorkModeEnum.None;
            return true;
        }

        private async Task Temperature(
            ITelegramBotClient bot,
            Telegram.Bot.Types.Message message,
            UserModel user,
            ActiveUserModel activeUserData,
            string text,
            CancellationToken ct)
        {

            if (!float.TryParse(text,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.GetCultureInfo("ru-RU"),
                out float result))
            {
                float.TryParse(text,
                     NumberStyles.Float | NumberStyles.AllowThousands,
                     CultureInfo.InvariantCulture,
                     out result);
            }

            if (result != default && result < 50f && result > 20f)
            {
                var analis = new Analysis(AnalysisTypeEnum.Temperature)
                {
                    DateTime = DateTime.Now,
                    UserId = user.Id,
                    Info = AnalysisTypeEnum.Temperature.GetDescription(),
                    Indicators = $"t = {result}",
                    Recommendations = string.Empty
                };

                _ = await _repository.AddAnalysisAsync(analis);
            }

            await SendStartMenu(bot, message, user, ct);
            activeUserData.WorkMode = TelegramWorkModeEnum.None;
            _awaitUserDataChatId = default;
        }

        private async Task Name(ITelegramBotClient bot, Telegram.Bot.Types.Message responseMessage, CancellationToken ct)
        {
            var activeUserData = _userStatusService.GetActiveUserData(responseMessage.Chat.Id);
            if (activeUserData == null)
            {
                _logger.LogWarning($"Пользователь с Id чата'{responseMessage.Chat.Id}' не активен");
                return;
            }

            if (activeUserData.User == null)
            {
                activeUserData.User = new UserModel();
                activeUserData.User.SetChatId(responseMessage.Chat.Id);
                activeUserData.User.SetName(responseMessage.Text);
            }

            await bot.SendMessage(
                chatId: responseMessage.Chat.Id,
                text: "Выберите Ваш пол (напишите 'м'|'ж')",
                cancellationToken: ct);

            activeUserData.WorkMode = TelegramWorkModeEnum.Gender;
            _awaitUserDataChatId = responseMessage.Chat.Id;
        }

        private async Task Gender(ITelegramBotClient bot, Telegram.Bot.Types.Message responseMessage, CancellationToken ct)
        {
            var activeUserData = _userStatusService.GetActiveUserData(responseMessage.Chat.Id);
            if (activeUserData == null)
            {
                _logger.LogWarning($"Пользователь с Id чата'{responseMessage.Chat.Id}' не активен");
                return;
            }

            GenderEnum gender = GenderEnum.Male;
            switch (responseMessage.Text.ToLower())
            {
                case "ж":
                    {
                        gender = GenderEnum.Female;
                        break;
                    }
                case "м":
                    {
                        gender = GenderEnum.Male;
                        break;
                    }
                default:
                    {
                        gender = GenderEnum.Unknown;
                        _logger.LogWarning($"Не удалось прочитать пол. ");
                        break;
                    }
            }

            activeUserData.User.SetGender(gender);

            await bot.SendMessage(
                chatId: responseMessage.Chat.Id,
                text: "Сколько Вам полных лет",
                cancellationToken: ct);

            activeUserData.WorkMode = TelegramWorkModeEnum.Age;
            _awaitUserDataChatId = responseMessage.Chat.Id;
        }

        private async Task Age(ITelegramBotClient bot, Telegram.Bot.Types.Message responseMessage, CancellationToken ct)
        {
            var activeUserData = _userStatusService.GetActiveUserData(responseMessage.Chat.Id);
            if (activeUserData == null)
            {
                _logger.LogWarning($"Пользователь с Id чата'{responseMessage.Chat.Id}' не активен");
                return;
            }

            if (!int.TryParse(responseMessage.Text, out int age))
            {
                _logger.LogWarning($"{nameof(Age)} Не удалось прочитать введённое значение");
                return;
            }

            activeUserData.User.SetAge(age);

            await bot.SendMessage(
                chatId: responseMessage.Chat.Id,
                text: "Введите ваш рост",
                cancellationToken: ct);

            activeUserData.WorkMode = TelegramWorkModeEnum.Height;
            _awaitUserDataChatId = responseMessage.Chat.Id;
        }

        private async Task Weight(ITelegramBotClient bot, Telegram.Bot.Types.Message responseMessage, CancellationToken ct)
        {
            var activeUserData = _userStatusService.GetActiveUserData(responseMessage.Chat.Id);
            if (activeUserData == null)
            {
                _logger.LogWarning($"Пользователь с Id чата'{responseMessage.Chat.Id}' не активен");
                return;
            }

            if (!float.TryParse(responseMessage.Text, out float weight))
            {
                _logger.LogWarning($"{nameof(Weight)} Не удалось прочитать введённое значение");
                return;
            }

            activeUserData.User.SetWeight(weight);

            await bot.SendMessage(
                chatId: responseMessage.Chat.Id,
                text: "Вы зарегистрированы",
                cancellationToken: ct);

            activeUserData.WorkMode = TelegramWorkModeEnum.None;
            _awaitUserDataChatId = default;

            _ = await _repository.AddUserAsync(activeUserData.User);

            await SendStartMenu(bot, responseMessage, activeUserData.User, ct);
        }

        private async Task Height(ITelegramBotClient bot, Telegram.Bot.Types.Message responseMessage, CancellationToken ct)
        {
            var activeUserData = _userStatusService.GetActiveUserData(responseMessage.Chat.Id);
            if (activeUserData == null)
            {
                _logger.LogWarning($"Пользователь с Id чата'{responseMessage.Chat.Id}' не активен");
                return;
            }

            if (!float.TryParse(responseMessage.Text, out float height))
            {
                _logger.LogWarning($"{nameof(Weight)} Не удалось прочитать введённое значение");
                return;
            }

            activeUserData.User.SetHeight(height);

            await bot.SendMessage(
                chatId: responseMessage.Chat.Id,
                text: "Введите ваш вес",
                cancellationToken: ct);

            activeUserData.WorkMode = TelegramWorkModeEnum.Weight;
            _awaitUserDataChatId = responseMessage.Chat.Id;
        }

        #endregion

        #region Menu

        private async Task SendManualMenu(ITelegramBotClient bot, Telegram.Bot.Types.Message message, UserModel user, CancellationToken ct)
        {
            var activeUserData = _userStatusService.GetActiveUserData(message.Chat.Id);

            if (activeUserData == null)
            {
                _logger.LogWarning($"Пользователь с Id чата'{message.Chat.Id}' не активен");
                return;
            }

            if (activeUserData.Menu.Current != null && !activeUserData.Menu.IsBack)
            {
                activeUserData.Menu.Preview = activeUserData.Menu.Current;
            }

            CreateMenuModel createMenuModel = CreateManualMenu(message, user);

            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: createMenuModel.Description,
                replyMarkup: createMenuModel.Menu,
                cancellationToken: ct);

            activeUserData.Menu.Current = createMenuModel.Menu;
        }

        private CreateMenuModel CreateManualMenu(Telegram.Bot.Types.Message message, UserModel user)
        {
            CreateMenuModel createMenuModel = new();

            var activeUserData = _userStatusService.GetActiveUserData(message.Chat.Id);
            if (activeUserData == null)
            {
                _logger.LogWarning($"Пользователь с Id чата'{message.Chat.Id}' не активен");
                return createMenuModel;
            }

            if (user != null)
            {
                createMenuModel.Menu = _telegramMenuService.CreateInLineManualMenu(user, activeUserData.Menu);
                createMenuModel.Description = "Выберите пункт";
            }
            else
            {
                var ex = new Exception($"Попытка ручного ввода анализов незарегистрированным пользователем с Id чата {message.Chat.Id}");
                _logger.LogError(nameof(CreateManualMenu), ex);
                throw ex;
            }

            return createMenuModel;
        }

        private async Task SendStartMenu(ITelegramBotClient bot, Telegram.Bot.Types.Message message, UserModel user, CancellationToken ct)
        {
            var activeUserData = _userStatusService.GetActiveUserData(message.Chat.Id);

            if (activeUserData == null)
            {
                if (_userStatusService.TryAddActiveUser(message.Chat.Id, user, new MenuInLineKeyboardMarkup()))
                {
                    activeUserData = _userStatusService.GetActiveUserData(message.Chat.Id);
                }
            }

            if (activeUserData.Menu.Current != null && !activeUserData.Menu.IsBack)
            {
                activeUserData.Menu.Preview = activeUserData.Menu.Current;
            }

            CreateMenuModel createMenuModel = CreateMenu(message, user);

            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: createMenuModel.Description,
                replyMarkup: createMenuModel.Menu,
                cancellationToken: ct);

            activeUserData.Menu.Current = createMenuModel.Menu;
        }

        private async Task SendAnalysesMenu(ITelegramBotClient bot, Telegram.Bot.Types.Message message, CancellationToken ct)
        {
            var activeUserData = _userStatusService.GetActiveUserData(message.Chat.Id);
            if (activeUserData == null)
            {
                _logger.LogWarning($"Пользователь с Id чата'{message.Chat.Id}' не активен");
                return;
            }

            if (activeUserData.Menu.Current != null && !activeUserData.Menu.IsBack)
            {
                activeUserData.Menu.Preview = activeUserData.Menu.Current;
            }

            var createMenuModel = await CreateAnalysisMenu(message);

            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: createMenuModel.Description,
                replyMarkup: createMenuModel.Menu,
                cancellationToken: ct);

            activeUserData.Menu.Current = createMenuModel.Menu;
        }

        private CreateMenuModel CreateMenu(Telegram.Bot.Types.Message message, UserModel user)
        {
            CreateMenuModel createMenuModel = new();

            var activeUserData = _userStatusService.GetActiveUserData(message.Chat.Id);
            if (activeUserData == null)
            {
                _logger.LogWarning($"Пользователь с Id чата'{message.Chat.Id}' не активен");
                return createMenuModel;
            }

            if (user != null)
            {
                createMenuModel.Menu = _telegramMenuService.CreateInLineMainMenu(user, activeUserData.Menu, _chat.IsRunning());
                createMenuModel.Description = $"Добро пожаловать '{user.Name}'. Выберите действие:";
            }
            else
            {
                createMenuModel.Menu = _telegramMenuService.CreateUnregisteredUser(activeUserData.Menu);
                createMenuModel.Description = $"Добро пожаловать. Выберите действие:";
            }

            return createMenuModel;
        }

        private async Task<CreateMenuModel> CreateAnalysisMenu(Telegram.Bot.Types.Message message)
        {
            CreateMenuModel createMenuModel = new();

            var user = await _repository.GetUserByChatIdAsync(message.Chat.Id);

            var activeUserData = _userStatusService.GetActiveUserData(message.Chat.Id);
            if (activeUserData == null)
            {
                _logger.LogWarning($"Пользователь с Id чата'{message.Chat.Id}' не активен");
                return createMenuModel;
            }

            if (user != null)
            {
                createMenuModel.Menu = _telegramMenuService.CreateInLineAnalysisMenu(user, activeUserData.Menu);
                createMenuModel.Description = $"'{user.Name}'. Выберите действие:";
            }
            else
            {
                var ex = new Exception($"Попытка получения списка анализов незарегистрированным пользователем с Id чата {message.Chat.Id}");
                _logger.LogError(nameof(CreateAnalysisMenu), ex);
                throw ex;
            }

            return createMenuModel;
        }

        private async Task SendAnalysesHistoryMenu(ITelegramBotClient bot, Telegram.Bot.Types.Message message, CancellationToken ct)
        {
            var activeUserData = _userStatusService.GetActiveUserData(message.Chat.Id);
            if (activeUserData == null)
            {
                _logger.LogWarning($"Пользователь с Id чата'{message.Chat.Id}' не активен");
                return;
            }

            if (activeUserData.Menu.Current != null && !activeUserData.Menu.IsBack)
            {
                activeUserData.Menu.Preview = activeUserData.Menu.Current;
            }

            var createMenuModel = await CreateAnalysisHistoryMenu(message);

            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: createMenuModel.Description,
                replyMarkup: createMenuModel.Menu,
                cancellationToken: ct);

            activeUserData.Menu.Current = createMenuModel.Menu;
        }

        private async Task<CreateMenuModel> CreateAnalysisHistoryMenu(Telegram.Bot.Types.Message message)
        {
            CreateMenuModel createMenuModel = new();

            var user = await _repository.GetUserByChatIdAsync(message.Chat.Id);

            var activeUserData = _userStatusService.GetActiveUserData(message.Chat.Id);
            if (activeUserData == null)
            {
                _logger.LogWarning($"Пользователь с Id чата'{message.Chat.Id}' не активен");
                return createMenuModel;
            }

            if (user != null)
            {
                createMenuModel.Menu = _telegramMenuService.CreateInLineAnalysisHistoryMenu(user, activeUserData.Menu);
                createMenuModel.Description = $"'{user.Name}'. Выберите действие:";
            }
            else
            {
                var ex = new Exception($"Попытка получения списка анализов незарегистрированным пользователем с Id чата {message.Chat.Id}");
                _logger.LogError(nameof(CreateAnalysisMenu), ex);
                throw ex;
            }

            return createMenuModel;
        }

        private async Task SendExitMenu(ITelegramBotClient bot, Telegram.Bot.Types.Message message, UserModel user, CancellationToken ct)
        {
            var activeUserData = _userStatusService.GetActiveUserData(message.Chat.Id);

            if (activeUserData == null)
            {
                if (_userStatusService.TryAddActiveUser(message.Chat.Id, user, new MenuInLineKeyboardMarkup()))
                {
                    activeUserData = _userStatusService.GetActiveUserData(message.Chat.Id);
                }
            }

            if (activeUserData.Menu.Current != null && !activeUserData.Menu.IsBack)
            {
                activeUserData.Menu.Preview = activeUserData.Menu.Current;
            }

            CreateMenuModel createMenuModel = CreateExitMenu(message, user);

            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: createMenuModel.Description,
                replyMarkup: createMenuModel.Menu,
                cancellationToken: ct);

            activeUserData.Menu.Current = createMenuModel.Menu;
        }

        private CreateMenuModel CreateExitMenu(Telegram.Bot.Types.Message message, UserModel user)
        {
            CreateMenuModel createMenuModel = new();

            var activeUserData = _userStatusService.GetActiveUserData(message.Chat.Id);
            if (activeUserData == null)
            {
                _logger.LogWarning($"Пользователь с Id чата'{message.Chat.Id}' не активен");
                return createMenuModel;
            }

            if (user != null)
            {
                createMenuModel.Menu = _telegramMenuService.CreateInLineExitMenu(user, activeUserData.Menu, _chat.IsRunning());
                createMenuModel.Description = $"Подтвердите выход:";
            }
            else
            {
                var ex = new Exception($"Попытка выхода незарегистрированным пользователем с Id чата {message.Chat.Id}");
                _logger.LogError(nameof(CreateExitMenu), ex);
                throw ex;
            }

            return createMenuModel;
        }

        #endregion
    }
}
