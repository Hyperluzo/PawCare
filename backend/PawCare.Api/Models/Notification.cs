namespace PawCare.Api.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = default!;
        public int OwnerId { get; set; }
        public Owner Owner { get; set; } = default!;
        public string NotificationType { get; set; } = default!;
        public string RecipientEmail { get; set; } = default!;
        public string DeliveryStatus { get; set; } = "PENDING";
        public string? ErrorMessage { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}