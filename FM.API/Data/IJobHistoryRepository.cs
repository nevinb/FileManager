using FM.API.Data.Models;

namespace FM.API.Data
{
    public interface IJobHistoryRepository
    {
        Task<IEnumerable<JobExecutionHistory>> GetHistoryByJobIdAsync(string jobId, int limit = 10);
        Task<string> CreateHistoryEntryAsync(JobExecutionHistory history);
        Task<bool> UpdateHistoryEntryAsync(JobExecutionHistory history);
        Task<JobExecutionHistory?> GetHistoryEntryByIdAsync(string id);
    }
}