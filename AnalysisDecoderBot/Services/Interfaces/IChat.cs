using AnalysisDecoderBot.Models;

namespace AnalysisDecoderBot.Services.Interfaces
{
    public interface IChat : IDisposable
    {
        Task<string> SendTextMessageAsync(string message);

        Task<MedicalReportResponse> SendFileMessageAsync(string path, UserModel user);
        public void Start();

        public void Stop();

        public bool IsRunning();

        public IEnumerable<string> GetSupportedFilesFormat();
        int GetStatistics();
    }
}