namespace FM.API.Data.Models
{
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