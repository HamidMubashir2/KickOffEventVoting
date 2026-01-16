namespace KickOffEvent.Models
{
    public class Vote
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SessionId { get; set; }

        // Voter (logged-in)
        public int VoterUserId { get; set; }
        public string VoterName { get; set; } = "";

        // Candidate (the person who got vote)
        public int CandidateUserId { get; set; }
        public string CandidateName { get; set; } = "";

        // Category = "Male" or "Female" (vote kis category ka hai)
        public string Category { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    }
}
