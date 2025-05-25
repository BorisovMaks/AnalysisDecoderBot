using AnalysisDecoderBot.Enums;
using AnalysisDecoderBot.Models;
using AnalysisDecoderBot.Repositories;
using AnalysisDecoderBot.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Telegram.Bot.Types;

namespace AnalysisDecoderBot.Services.Implementations
{
    public class SqLiteRepository : ISqLiteRepository, IDisposable
    {
        private readonly UserRepository _userRepository;
        private readonly AnalysisRepository _analysisRepository;
        private readonly SqliteConnection _connection;
        private readonly ILogger _logger;

        public SqLiteRepository(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _connection = new SqliteConnection(GetConnectionString(configuration));
            _connection.Open();

            _userRepository = new UserRepository(_connection);
            _analysisRepository = new AnalysisRepository(_connection);
        }

        #region UserModel

        public async Task<int> AddUserAsync(UserModel userModel)
        {
            try
            {
                return await _userRepository.AddAsync(userModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(AddUserAsync), ex);
                throw;
            }
        }

        public async Task UpdateUserAsync(UserModel userModel)
        {
            try
            {
                await _userRepository.UpdateAsync(userModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateUserAsync), ex);
                throw;
            }
        }

        public async Task DeleteUserAsync(int id)
        {
            try
            {
                await _userRepository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteUserAsync), ex);
                throw;
            }
        }

        public async Task<IEnumerable<UserModel>> GetAllUsersAsync()
        {
            try
            {
                return await _userRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(GetAllUsersAsync), ex);
                throw;
            }
        }

        public async Task<UserModel> GetUserAsync(int id)
        {
            try
            {
                return await _userRepository.GetAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(GetUserAsync), ex);
                throw;
            }  
        }

        public async Task<UserModel> GetUserByChatIdAsync(long chatId)
        {
            try
            {
                return await _userRepository.GetUserByChatId(chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(GetUserByChatIdAsync), ex);
                throw;
            } 
        }

        #endregion

        #region AnalysisModel

        public async Task<int> AddAnalysisAsync(Analysis analysis)
        {
            try
            {
                return await _analysisRepository.AddAsync(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(AddAnalysisAsync), ex);
                throw;
            }
        }

        public async Task DeleteAnalysisAsync(int id)
        {
            try
            {
                await _analysisRepository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteAnalysisAsync), ex);
                throw;
            }
        }

        public async Task<AnalysisBase> GetAnalysisAsync(int id)
        {
            try
            {
                return await _analysisRepository.GetAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(GetAnalysisAsync), ex);
                throw;
            }
        }

        public async Task<IEnumerable<AnalysisBase>> GetAllAnalysisAsync()
        {
            try
            {
                return await _analysisRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(GetAllAnalysisAsync), ex);
                throw;
            }
        }

        public async Task UpdateAnalysisAsync(Analysis analysis)
        {
            try
            {
                await _analysisRepository.UpdateAsync(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateAnalysisAsync), ex);
                throw;
            }
        }

        public async Task<IEnumerable<Analysis>> GetUserAnalysisAsync(int userId, AnalysisTypeEnum analyzeType, int count = 3)
        {
            try
            {
                return await _analysisRepository.GetUserAnalysesAsync(userId, count, analyzeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(GetUserAnalysisAsync), ex);
                throw;
            }
        }

        public async Task<IEnumerable<Analysis>> GetAllUserAnalysesAsync(int userId)
        {
            try
            {
                return await _analysisRepository.GetAllUserAnalysesAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(GetAllUserAnalysesAsync), ex);
                throw;
            }
        }

        #endregion

        private string GetConnectionString(IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("ConnectionString");

            if (connectionString == null)
            {
                var ex = new Exception("Не удалось подключиться к БД");
                _logger.LogError(nameof(GetConnectionString), ex);
                throw ex;
            }

            return connectionString;
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
