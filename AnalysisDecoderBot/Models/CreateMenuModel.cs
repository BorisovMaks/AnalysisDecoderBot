using Telegram.Bot.Types.ReplyMarkups;

namespace AnalysisDecoderBot.Models
{
    public class CreateMenuModel
    {
        public InlineKeyboardMarkup Menu { get; set; }

        public string Description { get; set; }
    }
}
