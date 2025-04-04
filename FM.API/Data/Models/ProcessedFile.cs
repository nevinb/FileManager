using System;

namespace FM.API.Data.Models
{
    public class ProcessedFile
    {
        public int Id { get; set; }
        public int ConfigId { get; set; }
        public string FileName { get; set; }
        public string FileHash { get; set; }
        public DateTime FileModifiedDate { get; set; }
        public long FileSize { get; set; }
        public DateTime ProcessedDate { get; set; }
    }
}