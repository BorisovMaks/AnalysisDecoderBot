namespace AnalysisDecoderBot.Enums
{
    public enum GenderEnum : byte
    {
        Male = 0,
        Female = 1,
        Unknown = 2
    }

    public static class GenderExtensions
    {
        public static string GetDescription(this GenderEnum value)
        {
            string output = string.Empty;
            switch (value)
            {
                case GenderEnum.Male:
                    output = "Мужской";
                    break;
                case GenderEnum.Female:
                    output = "Женский";
                    break;
                case GenderEnum.Unknown:
                    output = "Не задан";
                    break;
            }

            return output;
        }
    }
}
