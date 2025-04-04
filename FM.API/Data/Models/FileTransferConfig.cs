using System;

namespace FM.API.Data.Models
{
    public class FileTransferConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SourceType { get; set; } // "DFS" or "SFTP"
        public string SourceLocation { get; set; }
        public string DestinationType { get; set; } // "DFS" or "SFTP" 
        public string DestinationLocation { get; set; }
        public string CronSchedule { get; set; }
        public bool IsEnabled { get; set; }
    }
}