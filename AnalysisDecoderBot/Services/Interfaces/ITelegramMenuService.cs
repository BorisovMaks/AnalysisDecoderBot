using AnalysisDecoderBot.Models;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnalysisDecoderBot.Services.Interfaces
{
    public interface ITelegramMenuService
    {
        InlineKeyboardMarkup CreateInLineMainMenu(UserModel user, MenuInLineKeyboardMarkup menu, bool chatIsRunning);
        InlineKeyboardMarkup CreateUnregisteredUser(MenuInLineKeyboardMarkup menu);
        InlineKeyboardMarkup CreateInLineAnalysisMenu(UserModel user, MenuInLineKeyboardMarkup menu);
        InlineKeyboardMarkup CreateInLineManualMenu(UserModel user, MenuInLineKeyboardMarkup menu);
        InlineKeyboardMarkup CreateInLineAnalysisHistoryMenu(UserModel user, MenuInLineKeyboardMarkup menu);
        InlineKeyboardMarkup CreateInLineExitMenu(UserModel user, MenuInLineKeyboardMarkup menu, bool v);
    }
}