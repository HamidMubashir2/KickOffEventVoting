namespace KickOffEventVoting.DTO
{
    public class VoteCastingRequest
    {
        public int CandidateUserId { get; set; }
        public Guid SessionId { get; set; }
    }
}
