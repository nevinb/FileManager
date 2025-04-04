using FM.API.Data.Models;

namespace FM.API.Data
{
    public interface IJobRepository
    {
        Task<IEnumerable<FileTransferJob>> GetAllJobsAsync();
        Task<FileTransferJob?> GetJobByIdAsync(string id);
        Task<string> CreateJobAsync(FileTransferJob job);
        Task<bool> UpdateJobAsync(FileTransferJob job);
        Task<bool> DeleteJobAsync(string id);
        Task<IEnumerable<FileTransferJob>> GetActiveJobsAsync();
    }
}