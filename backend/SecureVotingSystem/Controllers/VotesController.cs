using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SecureVotingSystem.Services;

namespace SecureVotingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VotesController : ControllerBase
    {
        private readonly IVoteService _voteService;

        public VotesController(IVoteService voteService)
        {
            _voteService = voteService;
        }

        [Authorize(Roles = "Voter")]
        [HttpPost]
        public async Task<IActionResult> Cast([FromBody] dynamic payload)
        {
            int electionId = (int)payload.electionId;
            int candidateId = (int)payload.candidateId;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim);

            var ok = await _voteService.CastVoteAsync(userId, electionId, candidateId);
            if (!ok) return BadRequest(new { error = "Could not cast vote (duplicate or inactive)" });
            return Ok(new { success = true });
        }
    }
}
