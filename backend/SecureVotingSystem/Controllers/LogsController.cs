using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVotingSystem.Data;

namespace SecureVotingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class LogsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public LogsController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public IActionResult Get()
        {
            var logs = _db.ActivityLogs.OrderByDescending(l => l.CreatedAt).Take(500).ToList();
            return Ok(logs);
        }
    }
}
