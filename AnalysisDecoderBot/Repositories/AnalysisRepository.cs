using AnalysisDecoderBot.Enums;
using AnalysisDecoderBot.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AnalysisDecoderBot.Repositories
{
    public class AnalysisRepository : IRepository<Analysis>
    {
        private readonly SqliteConnection _connection;
        public AnalysisRepository(SqliteConnection connection)
        {
            _connection = connection;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            
            var createAnalysisTable = @"
        CREATE TABLE IF NOT EXISTS Analyses (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL, 
            DateTime TEXT NOT NULL,
            Recommendations TEXT,
            Indicators TEXT,
            Info TEXT,
            AnalysisType INTEGER,
            FOREIGN KEY(UserId) REFERENCES Users(Id)
        )";

            var createChatIdIndex = @"
        CREATE INDEX IF NOT EXISTS IX_Users_ChatId ON Users(ChatId)";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = createAnalysisTable;
                command.ExecuteNonQuery();

                command.CommandText = createChatIdIndex;
                command.ExecuteNonQuery();
            }
        }

        public async Task<int> AddAsync(Analysis value)
        {
            string query = @"
        INSERT INTO Analyses (UserId, DateTime, Recommendations, Indicators, Info, AnalysisType)
        VALUES (@UserId, @DateTime, @Recommendations, @Indicators, @Info, @AnalysisType);
        SELECT last_insert_rowid();";

            var id = await _connection.ExecuteScalarAsync<int>(query, new
            {
                value.UserId,
                DateTime = value.DateTime.ToString("o"),
                value.Recommendations,
                value.Indicators,
                value.Info,
                value.AnalysisType
            });

            value.SetId(id);
            return id;
        }

        public async Task DeleteAsync(int id)
        {
            string sql = "DELETE FROM Analyses WHERE Id = @id";
            await _connection.ExecuteAsync(sql, new { id });
        }

        public async Task<Analysis> GetAsync(int id)
        {
            string query = "SELECT * FROM Analyses WHERE Id = @id";
            var result = await _connection.QueryFirstOrDefaultAsync<Analysis>(query, new { id });

            if (result != null)
            {
                if (DateTime.TryParse(result.DateTime.ToString(), out var dateTime))
                {
                    result.DateTime = dateTime;
                }
            }

            return result;
        }

        public async Task<IEnumerable<Analysis>> GetAllAsync()
        {
            string query = "SELECT * FROM Analyses";
            var results = await _connection.QueryAsync<Analysis>(query);

            foreach (var result in results)
            {
                if (DateTime.TryParse(result.DateTime.ToString(), out var dateTime))
                {
                    result.DateTime = dateTime;
                }
            }

            return results;
        }

        public async Task UpdateAsync(Analysis value)
        {
            string sql = @"
        UPDATE Analyses 
        SET UserId = @UserId, 
            DateTime = @DateTime, 
            Recommendations = @Recommendations, 
            Indicators = @Indicators, 
            Info = @Info,
            AnalysisType = @AnalysisType
        WHERE Id = @Id";

            await _connection.ExecuteAsync(sql, new
            {
                value.Id,
                value.UserId,
                DateTime = value.DateTime.ToString("o"),
                value.Recommendations,
                value.Indicators,
                value.Info
            });
        }

        public async Task<IEnumerable<Analysis>> GetUserAnalysesAsync(int userId, int count, AnalysisTypeEnum analysisType)
        {
            string query = "SELECT * FROM Analyses WHERE (UserId = @userId) AND (AnalysisType = @analysisType) ORDER BY DateTime DESC";
            var results = await _connection.QueryAsync<Analysis>(query, new { userId, analysisType });

            foreach (var result in results.Take(count))
            {
                if (DateTime.TryParse(result.DateTime.ToString(), out var dateTime))
                {
                    result.DateTime = dateTime;
                }
            }

            return results;
        }

        public async Task<IEnumerable<Analysis>> GetAllUserAnalysesAsync(int userId)
        {
            string query = "SELECT * FROM Analyses WHERE (UserId = @userId)  ORDER BY DateTime DESC";
            var results = await _connection.QueryAsync<Analysis>(query, new { userId });

            foreach (var result in results)
            {
                if (DateTime.TryParse(result.DateTime.ToString(), out var dateTime))
                {
                    result.DateTime = dateTime;
                }
            }

            return results;
        }
    }
}
