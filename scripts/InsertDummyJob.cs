using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Dapper;

namespace FM.Scripts
{
    public class InsertDummyData
    {
        public static async Task Main()
        {
            // Connection string to the SQLite database
            string connectionString = "Data Source=/Users/nevinbhaskaran/Projects/FIleManager/FM.API/app.db";

            // Create a job with the specified parameters
            var job = new FileTransferJob
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Sample DFS Transfer Job",
                Description = "Transfer files from From to To directory",
                SourceType = "DFS",
                SourcePath = "/Users/nevinbhaskaran/Projects/From",
                DestinationType = "DFS",
                DestinationPath = "/Users/nevinbhaskaran/Projects/To",
                ScheduleInSeconds = 10,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Insert the job into the database
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                
                // SQL query to insert the job
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

                await connection.ExecuteAsync(sql, job);
                
                Console.WriteLine($"Inserted job with ID: {job.Id}");
                Console.WriteLine($"Job Name: {job.Name}");
                Console.WriteLine($"Source Path: {job.SourcePath}");
                Console.WriteLine($"Destination Path: {job.DestinationPath}");
            }
        }
    }

    // Class matching the structure in the database
    public class FileTransferJob
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string SourceType { get; set; } = string.Empty; // "DFS" or "SFTP"
        public string SourcePath { get; set; } = string.Empty;
        public string? SourceCredentials { get; set; }
        public string DestinationType { get; set; } = string.Empty; // "DFS" or "SFTP"
        public string DestinationPath { get; set; } = string.Empty;
        public string? DestinationCredentials { get; set; }
        public int ScheduleInSeconds { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedDate { get; set; }
        public DateTime? LastRunDate { get; set; }
    }
}