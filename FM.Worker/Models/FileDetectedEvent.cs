using System;

namespace FM.Worker.Models
{
    public class FileDetectedEvent
    {
        public required string TenantId { get; set; }
        public int ConfigId { get; set; }
        public required string FilePath { get; set; }
        public required string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public required string SourceType { get; set; }
        public required string DestinationType { get; set; }
        public required string DestinationLocation { get; set; }
        public DateTimeOffset DetectedAt { get; set; }
    }
}