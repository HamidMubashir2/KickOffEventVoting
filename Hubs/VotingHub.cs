using KickOffEvent.Models;
using KickOffEventVoting.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KickOffEvent.Hubs
{
    [Authorize]
    public class VotingHub : Hub
    {
        private readonly AppDbContext _db;
    
        public VotingHub(AppDbContext db) => _db = db;

        private int GetUserId()
        {
            var idStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? Context.User?.FindFirstValue("sub");
            if (!int.TryParse(idStr, out var userId))
                throw new HubException("Invalid UserId");
            return userId;
        }

        private string GetUserName()
        {
            // Best: put plaintext name in JWT as ClaimTypes.Name
            return Context.User?.FindFirstValue(ClaimTypes.Name)
                   ?? Context.User?.FindFirstValue("name")
                   ?? "Unknown";
        }



        public async Task SendUsersList()
        {
            var session = await _db.VotingSessions.FirstOrDefaultAsync(s => s.IsActive);
            if (session == null)
            {
                await Clients.Caller.SendAsync("UsersList", new
                {
                    isActive = false,
                    users = new object[] { }
                });
                return;
            }

            var users = await _db.Users
                .Select(u => new
                {
                    id = u.Id,
                    name = u.UserName,
                    gender = u.Gender
                })
                .ToListAsync();

            await Clients.Group(session.Id.ToString())
                .SendAsync("UsersList", new
                {
                    isActive = true,
                    sessionId = session.Id,
                    users
                });
        }

        private async Task<VotingSession> GetActiveSessionOrThrow()
        {
            var session = await _db.VotingSessions.FirstOrDefaultAsync(s => s.IsActive);
            if (session == null) throw new HubException("No active session");

            if (DateTime.UtcNow > session.EndAtUtc)
            {
                session.IsActive = false;
                await _db.SaveChangesAsync();

                await BroadcastSessionStatus(session);
                throw new HubException("Time Over!");
            }

            return session;
        }
        private async Task BroadcastSessionStatus(VotingSession s)
        {
            await Clients.All.SendAsync("SessionUpdated", new
            {
                id = s.Id,
                isActive = s.IsActive,
                startAtUtc = s.StartAtUtc,
                endAtUtc = s.EndAtUtc
            });
        }
        // ✅ Frontend connects then calls JoinActiveSession()
        //public async Task JoinActiveSession()
        //{
        //    var session = await GetActiveSessionOrThrow();
        //    await Groups.AddToGroupAsync(Context.ConnectionId, session.Id.ToString());


        //    // 1️⃣ Send candidates immediately
        //    var candidates = await BuildCandidatesList(session.Id, GetUserId());
        //    await Clients.Caller.SendAsync("CandidatesList", candidates);

        //    // send current result immediately
        //    var result = await BuildResult(session.Id);
        //    await Clients.Caller.SendAsync("ResultUpdated", result);
        //}

        public async Task CastVote(int malePersonId, int femalePersonId)
        {
            try
            {
                var session = await GetActiveSessionOrThrow();

                var voterId = GetUserId();
                var voterName = GetUserName();

                // ❌ Already voted check (HARD LOCK)
                var alreadyVoted = await _db.Votes.AnyAsync(v =>
                    v.SessionId == session.Id &&
                    v.VoterUserId == voterId);

                if (alreadyVoted)
                    throw new HubException("You have already cast your vote.");

                // 🔎 Load both candidates
                var candidates = await _db.Users
                    .Where(u => u.Id == malePersonId || u.Id == femalePersonId)
                    .ToListAsync();

                if (candidates.Count != 2)
                    throw new HubException("Invalid candidates selected.");

                // ❌ Self vote check
                if (candidates.Any(c => c.Id == voterId))
                    throw new HubException("You cannot vote yourself.");

                var male = candidates.FirstOrDefault(c =>
                    c.Gender.Equals("Male", StringComparison.OrdinalIgnoreCase));

                var female = candidates.FirstOrDefault(c =>
                    c.Gender.Equals("Female", StringComparison.OrdinalIgnoreCase));

                if (male == null || female == null)
                    throw new HubException("You must vote exactly 1 Male and 1 Female.");

                // ✅ INSERT MALE VOTE
                _db.Votes.Add(new Vote
                {
                    SessionId = session.Id,
                    VoterUserId = voterId,
                    VoterName = voterName,
                    CandidateUserId = male.Id,
                    CandidateName = male.UserName,
                    Category = male.Gender,
                    CreatedAtUtc = DateTime.UtcNow
                });

                // ✅ INSERT FEMALE VOTE
                _db.Votes.Add(new Vote
                {
                    SessionId = session.Id,
                    VoterUserId = voterId,
                    VoterName = voterName,
                    CandidateUserId = female.Id,
                    CandidateName = female.UserName,
                    Category = male.Gender,
                    CreatedAtUtc = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();

                // 🔥 Realtime result broadcast
                var result = await BuildResult(session.Id);

                await Clients.Group(session.Id.ToString())
                    .SendAsync("ResultUpdated", result);
            }
            catch (Exception ex)
            {
                // 🔥 THIS LINE WILL TELL US EVERYTHING
                Console.WriteLine("❌ CastVote SERVER ERROR:");
                Console.WriteLine(ex.ToString());

                // send readable error to frontend
                throw new HubException(ex.Message);
            }
        }


        private async Task<object> BuildResult(Guid sessionId)
        {
            var votes = await _db.Votes
            .Where(v => v.SessionId == sessionId)
            .ToListAsync();

            var male = votes
                .Where(v => v.Category.Equals("Male", StringComparison.OrdinalIgnoreCase))
                .GroupBy(v => v.CandidateUserId)
                .Select(g => new { candidateUserId = g.Key, name = g.First().CandidateName, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToList();

            var female = votes
                .Where(v => v.Category.Equals("Female", StringComparison.OrdinalIgnoreCase))
                .GroupBy(v => v.CandidateUserId)
                .Select(g => new { candidateUserId = g.Key, name = g.First().CandidateName, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToList();

            return new
            {
                KingList = male,
                QueenList = female,
                TotalVotes = votes.Count
            };
        }

        private async Task<object> BuildCandidatesList(Guid sessionId, int? excludeUserId = null)
        {
            var users = await _db.Users
                .Where(u => excludeUserId == null || u.Id != excludeUserId)
                .Select(u => new { u.Id, u.UserName, u.Gender })
                .ToListAsync();

            var male = users
                .Where(u => u.Gender == "Male")
                .Select(u => new { id = u.Id, name = u.UserName });

            var female = users
                .Where(u => u.Gender == "Female")
                .Select(u => new { id = u.Id, name = u.UserName });

            return new
            {
                isActive = true,
                sessionId,
                male,
                female
            };
        }
        public async Task JoinActiveSession()
        {
            var session = await _db.VotingSessions
                .FirstOrDefaultAsync(s => s.IsActive);

            if (session == null)
                return;

            // 🔥 Add this connection to session group
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                session.Id.ToString()
            );
        }

    }

}
