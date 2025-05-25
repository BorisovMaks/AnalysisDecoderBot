using AnalysisDecoderBot.Models;
using AnalysisDecoderBot.Services.Implementations;
using Microsoft.Extensions.Configuration;
using SQLitePCL;

namespace AnalysisDecoderBot
{
    class Program
    {
        private static SqLiteRepository _sqLiteRepository;
        private static TelegramMessenger _messenger;
        private static ILogger _logger;
        private static UserStatusService _userStatusService;
        static async Task Main(string[] args)
        {
            _logger = new Logger();

            IConfiguration configuration = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            Batteries.Init();

            _sqLiteRepository = new SqLiteRepository(
                _logger, 
                configuration);

            _userStatusService = new UserStatusService(_logger);

            _messenger = new TelegramMessenger(
                _logger, 
                _sqLiteRepository, 
                configuration,
                _userStatusService);

            await _messenger.StartAsync();

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

            await Task.Delay(-1);
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (_sqLiteRepository != null)
            {
                _sqLiteRepository.Dispose();
            }

            if (_messenger != null)
            {
                Task.Run(_messenger.StopAsync);
            }

            if (_userStatusService != null)
            {
                _userStatusService.Dispose();
            }

            _logger.LogInfo("Окончание работы приложения");
        }
    }
}
