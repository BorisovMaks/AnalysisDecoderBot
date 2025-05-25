namespace AnalysisDecoderBot.Repositories
{
    public interface IRepository<T>
    {
        public Task<int> AddAsync(T value);
        public Task UpdateAsync(T value);
        public Task DeleteAsync(int id);
        public Task<IEnumerable<T>> GetAllAsync();
        public Task<T> GetAsync(int id);
    }
}
