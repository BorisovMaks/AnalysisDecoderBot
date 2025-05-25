using AnalysisDecoderBot.Enums;
using System.Text;

namespace AnalysisDecoderBot.Models
{
    public class Analysis : AnalysisBase
    {
        public string Recommendations { get; set; }
        public string Indicators { get; set; }
        public string Info { get; set; }
        public AnalysisTypeEnum AnalysisType { get; }

        public Analysis()
        {
                
        }

        public Analysis(AnalysisTypeEnum analysisType)
        {
            AnalysisType = analysisType;
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine(Info);
            sb.AppendLine(Indicators);
            sb.AppendLine(Recommendations);

            return sb.ToString();
        }
    }
}