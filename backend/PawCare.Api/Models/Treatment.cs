namespace PawCare.Api.Models
{
    public class Treatment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = default!;
        public string Description { get; set; } = default!;
        public int AddedByUserId { get; set; }
        public User AddedByUser { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}