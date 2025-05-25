namespace AnalysisDecoderBot.Models
{
    public abstract class ModelBase
    {
        public int Id { get; private set; }

        public void SetId(int id)
        {
            Id = id;
        }
    }
}
