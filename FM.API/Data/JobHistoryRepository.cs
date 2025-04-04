using Dapper;
using FM.API.Data.Models;
using Microsoft.Data.Sqlite;

namespace FM.API.Data
{
    public class JobHistoryRepository : IJobHistoryRepository
    {
        private readonly SqliteConnection _connection;
        private readonly ILogger<JobHistoryRepository> _logger;

        public JobHistoryRepository(SqliteConnection connection, ILogger<JobHistoryRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<IEnumerable<JobExecutionHistory>> GetHistoryByJobIdAsync(string jobId, int limit = 10)
        {
            try
            {
                var sql = "SELECT * FROM JobExecutionHistory WHERE JobId = @JobId ORDER BY StartTime DESC LIMIT @Limit";
                return await _connection.QueryAsync<JobExecutionHistory>(sql, new { JobId = jobId, Limit = limit });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving history for job {JobId}", jobId);
                return Enumerable.Empty<JobExecutionHistory>();
            }
        }

        public async Task<string> CreateHistoryEntryAsync(JobExecutionHistory history)
        {
            try
            {
                var sql = @"
                    INSERT INTO JobExecutionHistory (
                        Id, JobId, StartTime, EndTime, Status, Message, SourceFile, DestinationFile
                    ) VALUES (
                        @Id, @JobId, @StartTime, @EndTime, @Status, @Message, @SourceFile, @DestinationFile
                    )";

                await _connection.ExecuteAsync(sql, history);
                return history.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating history entry for job {JobId}", history.JobId);
                throw;
            }
        }

        public async Task<bool> UpdateHistoryEntryAsync(JobExecutionHistory history)
        {
            try
            {
                var sql = @"
                    UPDATE JobExecutionHistory SET
                        JobId = @JobId,
                        StartTime = @StartTime,
                        EndTime = @EndTime,
                        Status = @Status,
                        Message = @Message,
                        SourceFile = @SourceFile,
                        DestinationFile = @DestinationFile
                    WHERE Id = @Id";

                var rowsAffected = await _connection.ExecuteAsync(sql, history);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating history entry {Id}", history.Id);
                return false;
            }
        }

        public async Task<JobExecutionHistory?> GetHistoryEntryByIdAsync(string id)
        {
            try
            {
                var sql = "SELECT * FROM JobExecutionHistory WHERE Id = @Id";
                return await _connection.QuerySingleOrDefaultAsync<JobExecutionHistory>(sql, new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving history entry with ID {Id}", id);
                return null;
            }
        }
    }
}