namespace PawCare.Api.Models
{
    public class Patient
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Species { get; set; } = default!;
        public string Breed { get; set; } = default!;
        public int Age { get; set; }
        public string Gender { get; set; } = default!;
        public int OwnerId { get; set; }
        public Owner Owner { get; set; } = default!;
        public int WardId { get; set; }
        public Ward Ward { get; set; } = default!;
        public string AdmissionStatus { get; set; } = "ADMITTED";
        public string ClinicalStatus { get; set; } = "UNDER_TREATMENT";
        public DateTime AdmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DischargedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}