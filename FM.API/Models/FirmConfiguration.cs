namespace FM.API.Models
{
    public class FirmConfiguration
    {
        public string FirmCode { get; set; } = string.Empty;
        public FirmDatabaseMode DatabaseMode { get; set; } = FirmDatabaseMode.Shared;
        public string ConnectionStringName { get; set; } = string.Empty;
        public string Schema { get; set; } = "dbo";
        public string[] AllowedClientCodes { get; set; } = Array.Empty<string>();
    }

    public enum FirmDatabaseMode
    {
        Dedicated,  // Firm has its own database
        Shared      // Firm shares a database with others
    }
}