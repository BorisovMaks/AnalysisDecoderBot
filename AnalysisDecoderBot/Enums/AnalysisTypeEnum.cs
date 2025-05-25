namespace AnalysisDecoderBot.Enums
{
    public enum AnalysisTypeEnum
    {
        Default = 0,
        Temperature = 1,
        Pressure = 2
    }

    public static class AnalysisTypeEnumExtension
    {
        public static string GetDescription(this AnalysisTypeEnum analysisType)
        {
            string output = string.Empty;
            switch (analysisType)
            {
                case AnalysisTypeEnum.Default:
                    output = "Анализ";
                    break;
                case AnalysisTypeEnum.Temperature:
                    output = "Температура тела";
                    break;
                case AnalysisTypeEnum.Pressure:
                    output = "Артериальное давление";
                    break;
            }
            return output;
        }
    }
}
