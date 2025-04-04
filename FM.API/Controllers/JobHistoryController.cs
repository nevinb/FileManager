using Microsoft.AspNetCore.Mvc;
using FM.API.Data;
using FM.API.Data.Models;

namespace FM.API.Controllers
{
    [ApiController]
    [Route("api/jobs/{jobId}/[controller]")]
    public class HistoryController : ControllerBase
    {
        private readonly IJobHistoryRepository _historyRepository;

        public HistoryController(IJobHistoryRepository historyRepository)
        {
            _historyRepository = historyRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetJobHistory(string jobId, [FromQuery] int limit = 10)
        {
            var history = await _historyRepository.GetHistoryByJobIdAsync(jobId, limit);
            return Ok(history);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetHistoryEntry(string jobId, string id)
        {
            var entry = await _historyRepository.GetHistoryEntryByIdAsync(id);
            
            if (entry == null || entry.JobId != jobId)
                return NotFound();
            
            return Ok(entry);
        }

        [HttpPost]
        public async Task<IActionResult> CreateHistoryEntry(string jobId, [FromBody] JobExecutionHistory history)
        {
            history.JobId = jobId;
            var id = await _historyRepository.CreateHistoryEntryAsync(history);
            return CreatedAtAction(nameof(GetHistoryEntry), new { jobId, id }, history);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHistoryEntry(string jobId, string id, [FromBody] JobExecutionHistory history)
        {
            history.Id = id;
            history.JobId = jobId;
            
            var result = await _historyRepository.UpdateHistoryEntryAsync(history);
            if (!result)
                return NotFound();
            
            return Ok();
        }
    }
}