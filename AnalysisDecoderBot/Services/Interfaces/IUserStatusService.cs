using AnalysisDecoderBot.Models;

namespace AnalysisDecoderBot.Services.Implementations
{
    public interface IUserStatusService : IDisposable
    {
        ActiveUserModel GetActiveUserData(long userChatId);
        int GetStatistics();
        bool TryAddActiveUser(long chatId, UserModel user, MenuInLineKeyboardMarkup currentUserMenu);
        bool TryRemoveActiveUser(long userChatId);
    }
}