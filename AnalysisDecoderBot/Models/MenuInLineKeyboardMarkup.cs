using Telegram.Bot.Types.ReplyMarkups;

namespace AnalysisDecoderBot.Models
{
    public class MenuInLineKeyboardMarkup 
    {
        public InlineKeyboardMarkup Current { get; set; }
        public InlineKeyboardMarkup Preview { get; set; }

        public bool CanShowPreview => Preview != null;
        public bool IsBack { get; set; } = false;
    }
}
