using AnalysisDecoderBot.Models;
using System.Collections.Concurrent;

namespace AnalysisDecoderBot.Services.Implementations
{
    public class UserStatusService : IUserStatusService
    {
        private ConcurrentDictionary<long, ActiveUserModel> _activeUsers;
        private readonly System.Timers.Timer _timer;
        private readonly ILogger _logger;
        public UserStatusService(ILogger logger)
        {
            _logger = logger;
            _activeUsers = new();

            _timer = new System.Timers.Timer(TimeSpan.FromMinutes(5));
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        public ActiveUserModel GetActiveUserData(long userChatId)
        {
            if (_activeUsers.TryGetValue(userChatId, out var activeUserModel))
            {
                return activeUserModel;
            }

            return null;
        }

        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (var user in _activeUsers)
            {
                if (DateTime.Now - user.Value.LastActive > TimeSpan.FromMinutes(15))
                {
                    _ = TryRemoveActiveUser(user.Key);
                    _logger.LogInfo($"Пользователь '{user.Key}' - отключен за бездействие!");
                }
            }
        }

        public bool TryAddActiveUser(long chatId, UserModel user, MenuInLineKeyboardMarkup currentUserMenu)
        {
            return _activeUsers.TryAdd(chatId, new ActiveUserModel()
            {
                User = user,
                Menu = currentUserMenu
            });
        }

        public bool TryRemoveActiveUser(long userChatId)
        {
            return _activeUsers.Remove(userChatId, out _);
        }

        public void Dispose()
        {
            _activeUsers.Clear();
            _timer.Stop();
            _timer?.Dispose();
        }

        public int GetStatistics()
        {
            return _activeUsers.Count;
        }
    }
}
