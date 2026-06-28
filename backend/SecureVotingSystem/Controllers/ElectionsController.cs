using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureVotingSystem.Data;

namespace SecureVotingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElectionsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public ElectionsController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _db.Elections.ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var e = await _db.Elections.FindAsync(id);
            if (e == null) return NotFound();
            var candidates = await _db.Candidates.Where(c => c.ElectionId == id).ToListAsync();
            return Ok(new { election = e, candidates });
        }
    }
}
