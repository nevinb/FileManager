using Dapper;
using FM.API.Data.Models;
using Microsoft.Data.Sqlite;

namespace FM.API.Data
{
    public class JobRepository : IJobRepository
    {
        private readonly SqliteConnection _connection;
        private readonly ILogger<JobRepository> _logger;

        public JobRepository(SqliteConnection connection, ILogger<JobRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<IEnumerable<FileTransferJob>> GetAllJobsAsync()
        {
            try
            {
                var sql = "SELECT * FROM FileTransferJobs";
                return await _connection.QueryAsync<FileTransferJob>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all jobs");
                return Enumerable.Empty<FileTransferJob>();
            }
        }

        public async Task<FileTransferJob?> GetJobByIdAsync(string id)
        {
            try
            {
                var sql = "SELECT * FROM FileTransferJobs WHERE Id = @Id";
                return await _connection.QuerySingleOrDefaultAsync<FileTransferJob>(sql, new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job with ID {JobId}", id);
                return null;
            }
        }

        public async Task<string> CreateJobAsync(FileTransferJob job)
        {
            try
            {
                var sql = @"
                    INSERT INTO FileTransferJobs (
                        Id, Name, Description, SourceType, SourcePath, SourceCredentials,
                        DestinationType, DestinationPath, DestinationCredentials,
                        ScheduleInSeconds, IsActive, CreatedDate, LastModifiedDate, LastRunDate
                    ) VALUES (
                        @Id, @Name, @Description, @SourceType, @SourcePath, @SourceCredentials,
                        @DestinationType, @DestinationPath, @DestinationCredentials,
                        @ScheduleInSeconds, @IsActive, @CreatedDate, @LastModifiedDate, @LastRunDate
                    )";

                await _connection.ExecuteAsync(sql, job);
                return job.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job {JobName}", job.Name);
                throw;
            }
        }

        public async Task<bool> UpdateJobAsync(FileTransferJob job)
        {
            try
            {
                job.LastModifiedDate = DateTime.UtcNow;

                var sql = @"
                    UPDATE FileTransferJobs SET
                        Name = @Name,
                        Description = @Description,
                        SourceType = @SourceType,
                        SourcePath = @SourcePath,
                        SourceCredentials = @SourceCredentials,
                        DestinationType = @DestinationType,
                        DestinationPath = @DestinationPath,
                        DestinationCredentials = @DestinationCredentials,
                        ScheduleInSeconds = @ScheduleInSeconds,
                        IsActive = @IsActive,
                        LastModifiedDate = @LastModifiedDate,
                        LastRunDate = @LastRunDate
                    WHERE Id = @Id";

                var rowsAffected = await _connection.ExecuteAsync(sql, job);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job {JobId}", job.Id);
                return false;
            }
        }

        public async Task<bool> DeleteJobAsync(string id)
        {
            try
            {
                var sql = "DELETE FROM FileTransferJobs WHERE Id = @Id";
                var rowsAffected = await _connection.ExecuteAsync(sql, new { Id = id });
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job {JobId}", id);
                return false;
            }
        }

        public async Task<IEnumerable<FileTransferJob>> GetActiveJobsAsync()
        {
            try
            {
                var sql = "SELECT * FROM FileTransferJobs WHERE IsActive = 1";
                return await _connection.QueryAsync<FileTransferJob>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active jobs");
                return Enumerable.Empty<FileTransferJob>();
            }
        }
    }
}