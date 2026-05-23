using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVotingSystem.Data;
using Microsoft.EntityFrameworkCore;
using SecureVotingSystem.DTOs;
using SecureVotingSystem.Models;

namespace SecureVotingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly SecureVotingSystem.Services.IActivityLogService _activityLog;
        public AdminController(ApplicationDbContext db, SecureVotingSystem.Services.IActivityLogService activityLog) { _db = db; _activityLog = activityLog; }

        [HttpPost("elections")]
        public async Task<IActionResult> CreateElection([FromBody] CreateElectionDto model)
        {
            if (model == null)
            {
                return BadRequest(new { error = "Election data is required." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var trimmedTitle = model.Title?.Trim() ?? string.Empty;
            var trimmedDescription = model.Description?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedTitle) || trimmedTitle.Length < 3)
            {
                return BadRequest(new { error = "Title must be at least 3 characters." });
            }

            if (string.IsNullOrWhiteSpace(trimmedDescription) || trimmedDescription.Length < 10)
            {
                return BadRequest(new { error = "Description must be at least 10 characters." });
            }

            var election = new Election
            {
                Title = trimmedTitle,
                Description = trimmedDescription,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                IsActive = false
            };

            try
            {
                _db.Elections.Add(election);
                await _db.SaveChangesAsync();
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(userIdClaim, out var adminId);
                await _activityLog.LogAsync(adminId, "CreateElection", $"Created election {election.Id}: {election.Title}");
                return Ok(election);
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { error = "Unable to create election. Please try again later." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An unexpected error occurred while creating the election." });
            }
        }

        [HttpPost("candidates")]
        public async Task<IActionResult> AddCandidate([FromBody] Candidate model)
        {
            _db.Candidates.Add(model);
            await _db.SaveChangesAsync();
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out var adminId);
            await _activityLog.LogAsync(adminId, "AddCandidate", $"Added candidate {model.Id} to election {model.ElectionId}");
            return Ok(model);
        }

        [HttpPost("elections/{id}/open")]
        public async Task<IActionResult> OpenElection(int id)
        {
            var e = await _db.Elections.FindAsync(id);
            if (e == null) return NotFound();
            e.IsActive = true;
            await _db.SaveChangesAsync();
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out var adminId);
            await _activityLog.LogAsync(adminId, "OpenElection", $"Opened election {id}");
            return Ok();
        }

        [HttpPost("elections/{id}/close")]
        public async Task<IActionResult> CloseElection(int id)
        {
            var e = await _db.Elections.FindAsync(id);
            if (e == null) return NotFound();
            e.IsActive = false;
            await _db.SaveChangesAsync();
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out var adminId);
            await _activityLog.LogAsync(adminId, "CloseElection", $"Closed election {id}");
            return Ok();
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var elections = await _db.Elections.ToListAsync();
            var users = await _db.Users.ToListAsync();
            var votes = await _db.Votes.ToListAsync();

            return Ok(new
            {
                totalElections = elections.Count,
                activeElections = elections.Count(e => e.IsActive),
                totalVoters = users.Count(u => u.Role == "Voter"),
                verifiedVoters = users.Count(u => u.Role == "Voter" && u.IsVerified),
                pendingVerifications = users.Count(u => u.Role == "Voter" && !u.IsVerified),
                totalVotes = votes.Count,
                electionResults = elections.Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.IsActive,
                    voteCount = votes.Count(v => v.ElectionId == e.Id)
                })
            });
        }

        [HttpGet("users")]
        public async Task<IActionResult> Users()
        {
            var users = await _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.IsVerified,
                    u.CreatedAt
                })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPost("users/{id}/verify")]
        public async Task<IActionResult> VerifyUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsVerified = true;
            await _db.SaveChangesAsync();
            return Ok(user);
        }

        [HttpGet("results/{electionId}")]
        public async Task<IActionResult> Results(int electionId)
        {
            var candidates = await _db.Candidates.Where(c => c.ElectionId == electionId).ToListAsync();
            var results = candidates.Select(c => new
            {
                candidate = c,
                votes = _db.Votes.Count(v => v.CandidateId == c.Id)
            });
            return Ok(results);
        }
    }
}
