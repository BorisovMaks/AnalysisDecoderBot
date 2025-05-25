namespace AnalysisDecoderBot.Models
{
    public class MedicalReportResponse
    {
        public string Recommendations { get; set; }
        public string Indicators { get; set; }
        public string Info { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}
