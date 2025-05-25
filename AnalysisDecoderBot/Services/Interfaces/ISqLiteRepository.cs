using AnalysisDecoderBot.Enums;
using AnalysisDecoderBot.Models;

namespace AnalysisDecoderBot.Services.Interfaces
{
    public interface ISqLiteRepository
    {
        Task<int> AddAnalysisAsync(Analysis analysis);
        Task<int> AddUserAsync(UserModel userModel);
        Task DeleteAnalysisAsync(int id);
        Task DeleteUserAsync(int id);
        void Dispose();
        Task<IEnumerable<AnalysisBase>> GetAllAnalysisAsync();
        Task<IEnumerable<UserModel>> GetAllUsersAsync();
        Task<AnalysisBase> GetAnalysisAsync(int id);
        Task<IEnumerable<Analysis>> GetUserAnalysisAsync(int userId, AnalysisTypeEnum analysisType, int cout = 3);
        Task<IEnumerable<Analysis>> GetAllUserAnalysesAsync(int userId);
        Task<UserModel> GetUserAsync(int id);
        Task<UserModel> GetUserByChatIdAsync(long chatId);
        Task UpdateAnalysisAsync(Analysis analysis);
        Task UpdateUserAsync(UserModel userModel);
    }
}