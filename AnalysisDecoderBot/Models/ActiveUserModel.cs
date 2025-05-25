using AnalysisDecoderBot.Enums;

namespace AnalysisDecoderBot.Models
{
    public class ActiveUserModel
    {   
        public UserModel User { get; set; }

        public MenuInLineKeyboardMarkup Menu { get; set; }

        public DateTime LastActive { get; set; }
        public TelegramWorkModeEnum WorkMode { get; set; } = TelegramWorkModeEnum.None;
    }
}
