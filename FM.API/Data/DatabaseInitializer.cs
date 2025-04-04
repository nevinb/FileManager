using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Dapper;

namespace FM.API.Data
{
    public class DatabaseInitializer
    {
        private readonly SqliteConnection _connection;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(SqliteConnection connection, ILogger<DatabaseInitializer> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing database");
            
            await _connection.OpenAsync();
            
            // Create FileTransferJobs table
            await _connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS FileTransferJobs (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    SourceType TEXT NOT NULL,
                    SourcePath TEXT NOT NULL,
                    SourceCredentials TEXT,
                    DestinationType TEXT NOT NULL,
                    DestinationPath TEXT NOT NULL,
                    DestinationCredentials TEXT,
                    ScheduleInSeconds INTEGER NOT NULL,
                    IsActive INTEGER NOT NULL,
                    CreatedDate TEXT NOT NULL,
                    LastModifiedDate TEXT,
                    LastRunDate TEXT
                )
            ");
            
            // Create JobExecutionHistory table
            await _connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS JobExecutionHistory (
                    Id TEXT PRIMARY KEY,
                    JobId TEXT NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT,
                    Status TEXT NOT NULL,
                    Message TEXT,
                    SourceFile TEXT,
                    DestinationFile TEXT,
                    FOREIGN KEY (JobId) REFERENCES FileTransferJobs (Id)
                )
            ");

            _logger.LogInformation("Database initialization completed");
        }
    }
}