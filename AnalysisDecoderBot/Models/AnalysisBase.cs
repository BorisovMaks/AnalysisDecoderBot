namespace AnalysisDecoderBot.Models
{
    public abstract class AnalysisBase : ModelBase
    {
        public int UserId { get; set; }
        public DateTime DateTime { get; set; }
    }
}
