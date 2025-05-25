namespace AnalysisDecoderBot.Models
{
    public enum LogLevel
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }

    public static class LogLevelExtensions
    { 
        public static string GetDescription(this LogLevel logLevel)
        {
            string output = string.Empty;
            switch (logLevel)
            {
                case LogLevel.Info:
                    output = "Информация";
                    break;
                case LogLevel.Warning:
                    output = "Предупреждение";
                    break;
                case LogLevel.Error:
                    output = "Ошибка";
                    break;
            }

            return output;
        }
    }

    public interface ILogger
    {
        void LogInfo(string message, LogLevel logLevel = LogLevel.Info, ConsoleColor foregroundColor = ConsoleColor.White);
        void LogWarning(string message, LogLevel logLevel = LogLevel.Warning, ConsoleColor foregroundColor = ConsoleColor.Yellow);
        void LogError(string message, Exception ex, LogLevel logLevel = LogLevel.Error, ConsoleColor foregroundColor = ConsoleColor.Red);
    }

    public class Logger : ILogger
    {
        public void LogInfo(
            string message,
            LogLevel logLevel = LogLevel.Info,
            ConsoleColor foregroundColor = ConsoleColor.Black)
        {
            ConsoleColor previewColor = Console.ForegroundColor;
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine($"'{logLevel.GetDescription()}':'{DateTime.Now}':'{message}'");
            Console.ForegroundColor = previewColor;
        }

        public void LogWarning(
            string message,
            LogLevel logLevel = LogLevel.Warning,
            ConsoleColor foregroundColor = ConsoleColor.Yellow)
        {
            ConsoleColor previewColor = Console.ForegroundColor;
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine($"'{logLevel.GetDescription()}':'{DateTime.Now}':'{message}'");
            Console.ForegroundColor = previewColor;
        }

        public void LogError(
            string message,
            Exception ex,
            LogLevel logLevel = LogLevel.Error,
            ConsoleColor foregroundColor = ConsoleColor.Red)
        {
            ConsoleColor previewColor = Console.ForegroundColor;
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine($"'{logLevel.GetDescription()}':'{DateTime.Now}':'{message}':\n '{ex}'");
            Console.ForegroundColor = previewColor;
        }
    }
}
