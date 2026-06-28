using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SecureVotingSystem.DTOs;
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
        public async Task<IActionResult> Cast([FromBody] VoteDto payload)
        {
            if (payload == null) return BadRequest(new { error = "Invalid vote request." });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim);

            var ok = await _voteService.CastVoteAsync(userId, payload.ElectionId, payload.CandidateId);
            if (!ok) return BadRequest(new { error = "Could not cast vote (duplicate or inactive)" });
            return Ok(new { success = true });
        }
    }
}
