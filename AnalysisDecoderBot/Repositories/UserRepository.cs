using AnalysisDecoderBot.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AnalysisDecoderBot.Repositories
{
    public class UserRepository : IRepository<UserModel>
    {
        private readonly SqliteConnection _connection;

        public UserRepository(SqliteConnection  connection)
        {
            _connection = connection;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            var createUserTable = @"
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ChatId INTEGER NOT NULL UNIQUE,
            Name TEXT NOT NULL,
            Gender INTEGER NOT NULL,
            Weight REAL,
            Height REAL,
            Age INTEGER,
            IsAdministrator INTEGER DEFAULT 0
        )";

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = createUserTable;
                command.ExecuteNonQuery();
            }
        }

        public async Task<int> AddAsync(UserModel value)
        {
            const string query = @"
        INSERT INTO Users (ChatId, Name, Gender, Weight, Height, Age, IsAdministrator)
        VALUES (@ChatId, @Name, @Gender, @Weight, @Height, @Age, @IsAdministrator);
        SELECT last_insert_rowid();";

            return await _connection.ExecuteScalarAsync<int>(query, value);
        }

        public async Task DeleteAsync(int id)
        {
            string query = "DELETE FROM Users WHERE Id = @id";
            await _connection.ExecuteAsync(query, new { id });
        }

        public async Task<UserModel> GetAsync(int id)
        {
            string query = "SELECT * FROM Users WHERE Id = @id";
            return await _connection.QueryFirstOrDefaultAsync<UserModel>(query, new { id });
        }

        public async Task<IEnumerable<UserModel>> GetAllAsync()
        {
            string query = "SELECT * FROM Users";
            return await _connection.QueryAsync<UserModel>(query);
        }

        public async Task UpdateAsync(UserModel value)
        {
            string query = @"
        UPDATE Users 
        SET ChatId = @ChatId, Name = @Name, Gender = @Gender, Weight = @Weight, 
            Height = @Height, Age = @Age, IsAdministrator = @IsAdministrator
        WHERE Id = @Id";

            await _connection.ExecuteAsync(query, value);
        }

        public async Task<UserModel> GetUserByChatId(long chatId)
        {
            string query = "SELECT * FROM Users WHERE ChatId = @chatId";
            return await _connection.QueryFirstOrDefaultAsync<UserModel>(query, new { chatId });
        }
    }
}
