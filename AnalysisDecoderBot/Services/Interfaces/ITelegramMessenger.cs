namespace AnalysisDecoderBot.Services.Interfaces
{
    public interface ITelegramMessenger
    {
        Task StartAsync();
        Task StopAsync();
    }
}