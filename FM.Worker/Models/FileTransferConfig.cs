using System;

namespace FM.Worker.Models
{
    public class FileTransferConfig
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string SourceType { get; set; } // "DFS" or "SFTP"
        public required string SourceLocation { get; set; }
        public required string DestinationType { get; set; } // "DFS" or "SFTP" 
        public required string DestinationLocation { get; set; }
        public required string CronSchedule { get; set; }
        public bool IsEnabled { get; set; }
    }
}