namespace PawCare.Api.Models
{
    public class StatusHistory
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = default!;
        public string OldStatus { get; set; } = default!;
        public string NewStatus { get; set; } = default!;
        public int ChangedByUserId { get; set; }
        public User ChangedByUser { get; set; } = default!;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}