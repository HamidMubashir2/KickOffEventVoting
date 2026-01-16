namespace KickOffEvent.Models
{
    public class VotingSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime StartAtUtc { get; set; }
        public DateTime EndAtUtc { get; set; }

        public bool IsActive { get; set; } = true;

        public int CreatedByUserId { get; set; }
    }
}
