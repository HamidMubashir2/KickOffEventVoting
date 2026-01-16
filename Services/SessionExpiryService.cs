using KickOffEvent.Hubs;
using KickOffEventVoting.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace KickOffEvent.Services
{
    public class SessionExpiryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<VotingHub> _hub;

        public SessionExpiryService(IServiceScopeFactory scopeFactory, IHubContext<VotingHub> hub)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var now = DateTime.UtcNow;

                var expired = await db.VotingSessions
                    .Where(s => s.IsActive && s.EndAtUtc <= now)
                    .ToListAsync();

                foreach (var s in expired)
                {
                    s.IsActive = false;

                    await _hub.Clients.All.SendAsync("SessionEnded", new
                    {
                        isActive = false,
                        id = s.Id,
                        message = "Time Over!"
                    });
                }

                if (expired.Any())
                    await db.SaveChangesAsync();

                await Task.Delay(5000, stoppingToken); // every 5 sec
            }
        }
        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    while (!stoppingToken.IsCancellationRequested)
        //    {
        //        using var scope = _scopeFactory.CreateScope();
        //        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        //        var now = DateTime.UtcNow;

        //        var expired = await db.VotingSessions
        //            .Where(s => s.IsActive && s.EndAtUtc <= now)
        //            .ToListAsync(stoppingToken);

        //        if (expired.Count > 0)
        //        {
        //            foreach (var s in expired) s.IsActive = false;
        //            await db.SaveChangesAsync(stoppingToken);

        //            foreach (var s in expired)
        //            {
        //                await _hub.Clients.Group(s.Id.ToString())
        //                    .SendAsync("SessionEnded",
        //                        new { sessionId = s.Id, message = "Time Over!" },
        //                        stoppingToken);
        //            }
        //        }

        //        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        //    }
        //}
    }
}
