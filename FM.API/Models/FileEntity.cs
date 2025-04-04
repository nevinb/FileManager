namespace FM.API.Models
{
    public class FileEntity
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string ClientCode { get; set; } = string.Empty;
    }
}