using Microsoft.AspNetCore.Mvc;
using FM.API.Data;
using FM.API.Data.Models;

namespace FM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly IJobRepository _jobRepository;

        public JobsController(IJobRepository jobRepository)
        {
            _jobRepository = jobRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllJobs()
        {
            var jobs = await _jobRepository.GetAllJobsAsync();
            return Ok(jobs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetJobById(string id)
        {
            var job = await _jobRepository.GetJobByIdAsync(id);
            if (job == null)
                return NotFound();
            
            return Ok(job);
        }

        [HttpPost]
        public async Task<IActionResult> CreateJob([FromBody] FileTransferJob job)
        {
            var id = await _jobRepository.CreateJobAsync(job);
            return CreatedAtAction(nameof(GetJobById), new { id }, job);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateJob(string id, [FromBody] FileTransferJob job)
        {
            job.Id = id;
            var result = await _jobRepository.UpdateJobAsync(job);
            if (!result)
                return NotFound();
            
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJob(string id)
        {
            var result = await _jobRepository.DeleteJobAsync(id);
            if (!result)
                return NotFound();
            
            return Ok();
        }
    }
}