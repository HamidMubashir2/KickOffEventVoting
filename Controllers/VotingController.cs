using KickOffEvent.DTO;
using KickOffEvent.Hubs;
using KickOffEvent.Models;
using KickOffEventVoting.Data;
using KickOffEventVoting.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KickOffEvent.Controller
{
    [ApiController]
    [Route("api/admin/sessions")]
    [Authorize]
    public class VotingController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<VotingHub> _hub;
        public VotingController(AppDbContext db, IHubContext<VotingHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        private int GetUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return int.Parse(idStr!);
        }

        private string GetUserName()
        {
            // Best: put plaintext name in JWT as ClaimTypes.Name
            return User?.FindFirstValue(ClaimTypes.Name)
                   ?? User?.FindFirstValue("name")
                   ?? "Unknown";
        }
        //// ✅ 1) START SESSION(Default 4 minutes)
        //[HttpPost("start-session")]
        //public async Task<IActionResult> StartSession()
        //{

        //    var old = await _db.VotingSessions.FirstOrDefaultAsync(s => s.IsActive);
        //    if (old != null) old.IsActive = false;

        //    var now = DateTime.UtcNow;

        //    var session = new VotingSession
        //    {
        //        StartAtUtc = now,
        //        EndAtUtc = now.AddMinutes(4),
        //        IsActive = true,
        //        CreatedByUserId = GetUserId()
        //    };

        //    _db.VotingSessions.Add(session);
        //    await _db.SaveChangesAsync();

        //    // 🔥 BROADCAST to everyone
        //    await _hub.Clients.All.SendAsync(
        //        "SessionStarted",
        //        new
        //        {
        //            sessionId = session.Id,
        //            startAtUtc = session.StartAtUtc,
        //            endAtUtc = session.EndAtUtc
        //        }
        //    );

        //    // 🔥 ALSO broadcast candidates list
        //    var users = await _db.Users
        //        .Select(u => new { u.Id, u.UserName, u.Gender })
        //        .ToListAsync();



        //    await _hub.Clients.All.SendAsync("CandidatesList", new
        //    {
        //        isActive = true,
        //        sessionId = session.Id,
        //        users
        //    });

        //    return Ok(session);
        //}


        // ✅ 2) ACTIVE SESSION (helper API)
        [HttpGet("active")]
        public async Task<IActionResult> Active()
        {
            var session = await _db.VotingSessions.FirstOrDefaultAsync(s => s.IsActive);

            if (session == null)
                return Ok(new { IsActive = false, Message = "No Active Session" });

            if (DateTime.UtcNow > session.EndAtUtc)
            {
                session.IsActive = false;
                await _db.SaveChangesAsync();

                await _hub.Clients.Group(session.Id.ToString())
                    .SendAsync("SessionEnded", new { SessionId = session.Id, Message = "Time Over!" });

                return Ok(new { IsActive = false, Message = "Time Over! Session Closed." });
            }

            return Ok(new
            {
                IsActive = true,
                session.Id,
                session.StartAtUtc,
                session.EndAtUtc
            });
        }

        // ✅ 3) Candidates list (frontend needs, excludes logged-in user)
        [HttpGet("Candidates")]
        public async Task<IActionResult> Candidates()
        {
            var session = await _db.VotingSessions.FirstOrDefaultAsync(s => s.IsActive);
            if (session == null) return Ok(new { isActive = false });

            var myId = GetUserId();

            var users = await _db.Users
                .Where(u => u.Id != myId)
                .Select(u => new { u.Id, u.UserName, u.Gender })
                .ToListAsync();

            var male = users
                .Where(x => (x.Gender ?? "").Equals("Male", StringComparison.OrdinalIgnoreCase))
                .Select(x => new { id = x.Id, name = x.UserName });

            var female = users
                .Where(x => (x.Gender ?? "").Equals("Female", StringComparison.OrdinalIgnoreCase))
                .Select(x => new { id = x.Id, name = x.UserName });

            return Ok(new { isActive = true, sessionId = session.Id, male, female });
        }

        // ✅ 4) Result API (Top 5)



        //[HttpPost("start-session")]
        //public async Task<IActionResult> StartSession()
        //{
        //    //if (!IsAdmin()) return Forbid();

        //    var old = await _db.VotingSessions.FirstOrDefaultAsync(s => s.IsActive);
        //    if (old != null) old.IsActive = false;

        //    var now = DateTime.UtcNow;

        //    var session = new VotingSession
        //    {
        //        StartAtUtc = now,
        //        EndAtUtc = now.AddMinutes(4),
        //        IsActive = true,
        //        CreatedByUserId = GetUserId()
        //    };

        //    _db.VotingSessions.Add(session);
        //    await _db.SaveChangesAsync();

        //    // 🔥 GET USERS
        //    var users = await _db.Users
        //        .Select(u => new
        //        {
        //            id = u.Id,
        //            name = u.UserName,
        //            gender = u.Gender
        //        })
        //        .ToListAsync();

        //    // 🔥 BROADCAST TO ALL CONNECTED USERS
        //    await _hub.Clients.All.SendAsync("UsersList", new
        //    {
        //        sessionId = session.Id,
        //        users
        //    });


        //    return Ok(session);
        //}

        //[HttpPost("start-session")]
        //public async Task<IActionResult> StartSession()
        //{
        //    var old = await _db.VotingSessions.FirstOrDefaultAsync(s => s.IsActive);
        //    if (old != null)
        //    {
        //        old.IsActive = false;
        //    }

        //    var now = DateTime.UtcNow;

        //    var session = new VotingSession
        //    {
        //        StartAtUtc = now,
        //        EndAtUtc = now.AddMinutes(4),
        //        IsActive = true,
        //        CreatedByUserId = GetUserId()
        //    };

        //    _db.VotingSessions.Add(session);
        //    await _db.SaveChangesAsync();

        //    // 🔥 REAL-TIME SESSION START
        //    await _hub.Clients.All.SendAsync("SessionStarted", new
        //    {
        //        isActive = session.IsActive,
        //        id = session.Id,
        //        startAtUtc = session.StartAtUtc,
        //        endAtUtc = session.EndAtUtc
        //    });

        //    // (optional) users list broadcast
        //    var users = await _db.Users
        //        .Select(u => new { id = u.Id, name = u.UserName, gender = u.Gender })
        //        .ToListAsync();

        //    //if (session.IsActive == tr)
        //        await _hub.Clients.All.SendAsync("UsersList", new
        //        {
        //            sessionId = session.Id,
        //            users
        //        });

        //    // REST response (admin ke liye)
        //    return Ok(new
        //    {
        //        isActive = true,
        //        id = session.Id,
        //        startAtUtc = session.StartAtUtc,
        //        endAtUtc = session.EndAtUtc
        //    });
        //}

        [HttpPost("start-session")]
        public async Task<IActionResult> StartSession()
        {
            // 1️⃣ Close any previous active session
            var old = await _db.VotingSessions.FirstOrDefaultAsync(s => s.IsActive);
            if (old != null)
            {
                old.IsActive = false;
                await _db.SaveChangesAsync();

                // 🔥 Broadcast old session closed
                await _hub.Clients.All.SendAsync("SessionUpdated", new
                {
                    id = old.Id,
                    isActive = false,
                    startAtUtc = old.StartAtUtc,
                    endAtUtc = old.EndAtUtc
                });
            }

            // 2️⃣ Create new session
            var now = DateTime.UtcNow;

            var session = new VotingSession
            {
                StartAtUtc = now,
                EndAtUtc = now.AddMinutes(4),
                IsActive = true,
                CreatedByUserId = GetUserId()
            };

            _db.VotingSessions.Add(session);
            await _db.SaveChangesAsync();

            // 3️⃣ Broadcast new session start
            await _hub.Clients.All.SendAsync("SessionUpdated", new
            {
                id = session.Id,
                isActive = true,
                startAtUtc = session.StartAtUtc,
                endAtUtc = session.EndAtUtc
            });

            // 4️⃣ Send users list with active=true
            var users = await _db.Users
                .Select(u => new { id = u.Id, name = u.UserName, gender = u.Gender })
                .ToListAsync();

            

            await _hub.Clients.All.SendAsync("UsersList", new
            {
                sessionId = session.Id,
                users,
                isActive = true
            });

            // 5️⃣ AUTO-EXPIRE TIMER (SERVER-DRIVEN)
            _ = Task.Run(async () =>
            {
                var delay = session.EndAtUtc - DateTime.UtcNow;
                if (delay.TotalMilliseconds > 0)
                    await Task.Delay(delay);

                using var scope = HttpContext.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var hub = scope.ServiceProvider.GetRequiredService<IHubContext<VotingHub>>();

                var s = await db.VotingSessions.FirstOrDefaultAsync(x => x.Id == session.Id);
                if (s != null && s.IsActive)
                {
                    s.IsActive = false;
                    await db.SaveChangesAsync();

                    // 🔥 Broadcast STOP
                    await hub.Clients.All.SendAsync("SessionUpdated", new
                    {
                        id = s.Id,
                        isActive = false,
                        startAtUtc = s.StartAtUtc,
                        endAtUtc = s.EndAtUtc
                    });

                    // 🔥 Update UsersList too
                    var liveUsers = await db.Users
                        .Select(u => new { id = u.Id, name = u.UserName, gender = u.Gender })
                        .ToListAsync();

                    await hub.Clients.All.SendAsync("UsersList", new
                    {
                        sessionId = s.Id,
                        users = liveUsers,
                        isActive = false
                    });
                }
            });

            return Ok(new
            {
                isActive = true,
                id = session.Id,
                startAtUtc = session.StartAtUtc,
                endAtUtc = session.EndAtUtc
            });
        }

        [HttpPost("VoteCasting")]
        public async Task<IActionResult> VoteCasting(List<VoteCastingRequest> voteReq)
        {
            foreach (var req in voteReq)
            {
                var session = await _db.VotingSessions.FirstOrDefaultAsync(s => s.Id == req.SessionId);
                if (session == null) throw new HubException("No active session");

                var voterId = GetUserId();
                var voterName = GetUserName();

                if (req.CandidateUserId == voterId)
                    throw new HubException("You cannot vote yourself");

                var candidate = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.CandidateUserId);
                if (candidate == null) throw new HubException("Invalid candidate");

                var category = (candidate.Gender ?? "").Trim();
                if (!category.Equals("Male", StringComparison.OrdinalIgnoreCase) &&
                    !category.Equals("Female", StringComparison.OrdinalIgnoreCase))
                    throw new HubException("Candidate gender invalid");

                var existing = await _db.Votes.FirstOrDefaultAsync(v =>
                   v.SessionId == session.Id &&
                   v.VoterUserId == voterId &&
                   v.Category == category);

                if (existing == null)
                {
                    _db.Votes.Add(new Vote
                    {
                        SessionId = session.Id,
                        VoterUserId = voterId,
                        VoterName = voterName,
                        CandidateUserId = candidate.Id,
                        CandidateName = candidate.UserName,
                        Category = category,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.CandidateUserId = candidate.Id;
                    existing.CandidateName = candidate.UserName;
                    existing.CreatedAtUtc = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
            }
           
            return Ok(new { Message = "Vote Casted Successfully" });
        }
    }
}
