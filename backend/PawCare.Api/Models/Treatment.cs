using System.ComponentModel.DataAnnotations.Schema;

namespace PawCare.Api.Models
{
    public class Treatment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = default!;

        public string Diagnosis { get; set; } = string.Empty;

        [Column("Treatment")]
        public string TreatmentPlan { get; set; } = string.Empty;

        public string Medication { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public string Description { get; set; } = string.Empty;

        public int AddedByUserId { get; set; }
        public User AddedByUser { get; set; } = default!;

        [NotMapped]
        public int UpdatedByUserId
        {
            get => AddedByUserId;
            set => AddedByUserId = value;
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}