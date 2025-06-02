using AnalysisDecoderBot.Models;
using AnalysisDecoderBot.Services.Interfaces;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnalysisDecoderBot.Services.Implementations
{
    public class TelegramMenuService : ITelegramMenuService
    {
        public InlineKeyboardMarkup CreateInLineMainMenu(UserModel user, MenuInLineKeyboardMarkup menu, bool chatIsRunning)
        {
            if (user.IsAdministrator)
            {
                return CreateAdministratoMainMenu(menu, chatIsRunning);
            }
            else
            {
                return CreateUserMainMenu(menu);
            }
        }

        public InlineKeyboardMarkup CreateUnregisteredUser(MenuInLineKeyboardMarkup menu)
        {
            return CreateUnregisteredUserMainMenu(menu);
        }

        private static InlineKeyboardMarkup CreateUnregisteredUserMainMenu(MenuInLineKeyboardMarkup menu)
        {
            List<InlineKeyboardButton> buttons =
            [
                InlineKeyboardButton.WithCallbackData("Контакты", BotCommands.Contacts),
            ];

            if (menu.CanShowPreview)
            {
                buttons.Add(InlineKeyboardButton.WithCallbackData("Назад", BotCommands.Back));
            }

            return new InlineKeyboardMarkup(
            [
                [
                    InlineKeyboardButton.WithCallbackData("Регистрация", BotCommands.SignUp),
                    InlineKeyboardButton.WithCallbackData("О программе", BotCommands.About)
                ],
                buttons
            ]);
        }


        private static InlineKeyboardMarkup CreateUserMainMenu(MenuInLineKeyboardMarkup menu)
        {
            List<InlineKeyboardButton> buttons =
            [
                InlineKeyboardButton.WithCallbackData("Контакты", BotCommands.Contacts),
            ];

            if (menu.CanShowPreview)
            {
                buttons.Add(InlineKeyboardButton.WithCallbackData("Назад", BotCommands.Back));
            }

            return new InlineKeyboardMarkup(
            [
                [
                    InlineKeyboardButton.WithCallbackData("Анализы", BotCommands.Analysis),
                    InlineKeyboardButton.WithCallbackData("О программе", BotCommands.About),
                    InlineKeyboardButton.WithCallbackData("Выйти", BotCommands.Exit),
                ],
                buttons
            ]);
        }

        private static InlineKeyboardMarkup CreateAdministratoMainMenu(MenuInLineKeyboardMarkup menu, bool chatIsRunning)
        {
            List<InlineKeyboardButton> buttons =
            [
                InlineKeyboardButton.WithCallbackData("Контакты", BotCommands.Contacts),
                InlineKeyboardButton.WithCallbackData("О программе", BotCommands.About)
            ];

            if (menu.CanShowPreview)
            {
                buttons.Add(InlineKeyboardButton.WithCallbackData("Назад", BotCommands.Back));
            }

            return new InlineKeyboardMarkup(
            [
                [
                    InlineKeyboardButton.WithCallbackData("Анализы", BotCommands.Analysis),
                    InlineKeyboardButton.WithCallbackData(chatIsRunning ?  "Остановить AI" : "Запустить AI", BotCommands.OnOff),
                    InlineKeyboardButton.WithCallbackData("Статистика", BotCommands.Statistics)
                ],
                buttons
            ]);
        }

        public InlineKeyboardMarkup CreateInLineAnalysisMenu(UserModel user, MenuInLineKeyboardMarkup menu)
        {
            return new InlineKeyboardMarkup(
            [
                [
                    InlineKeyboardButton.WithCallbackData("История анализов", BotCommands.History),
                    InlineKeyboardButton.WithCallbackData("Загрузить pdf", BotCommands.PDF)
                ],
                [
                    InlineKeyboardButton.WithCallbackData("Ввести вручную", BotCommands.Manual),
                    InlineKeyboardButton.WithCallbackData("Назад", BotCommands.Back)
                ]
            ]);
        }

        public InlineKeyboardMarkup CreateInLineManualMenu(UserModel user, MenuInLineKeyboardMarkup menu)
        {
            return new InlineKeyboardMarkup(
            [
                [
                    InlineKeyboardButton.WithCallbackData("Температура тела", BotCommands.Temperature),
                    InlineKeyboardButton.WithCallbackData("Артериальное давление", BotCommands.Pressure)
                ]
            ]);
        }

        public InlineKeyboardMarkup CreateInLineAnalysisHistoryMenu(UserModel user, MenuInLineKeyboardMarkup menu)
        {
            return new InlineKeyboardMarkup(
            [
                [
                    InlineKeyboardButton.WithCallbackData("Анализы", BotCommands.AnalysisHistory),
                    InlineKeyboardButton.WithCallbackData("Температура тела", BotCommands.TemperatureHistory),
                    InlineKeyboardButton.WithCallbackData("Артериальное давление", BotCommands.PressureHistory)
                ]
            ]);
        }

        public InlineKeyboardMarkup CreateInLineExitMenu(UserModel user, MenuInLineKeyboardMarkup menu, bool v)
        {
            return new InlineKeyboardMarkup(
            [
                [
                    InlineKeyboardButton.WithCallbackData("Подтвердить", BotCommands.Ok),
                    InlineKeyboardButton.WithCallbackData("Отменить", BotCommands.Cancel)
                ]
            ]);
        }
    }
}
