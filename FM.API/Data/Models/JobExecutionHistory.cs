namespace FM.API.Data.Models
{
    public class JobExecutionHistory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string JobId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty; // "Success", "Failed", "Running"
        public string? Message { get; set; }
        public string? SourceFile { get; set; }
        public string? DestinationFile { get; set; }
    }
}