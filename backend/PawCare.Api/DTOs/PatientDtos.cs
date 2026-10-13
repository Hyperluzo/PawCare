namespace PawCare.Api.DTOs
{
    public class OwnerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string? Address { get; set; }
    }

    public class CreateOwnerRequest
    {
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string? Address { get; set; }
    }

    public class AdmitPatientRequest
    {
        public string PatientName { get; set; } = default!;
        public string Species { get; set; } = default!;
        public string Breed { get; set; } = default!;
        public int Age { get; set; }
        public string Gender { get; set; } = default!;
        public int WardId { get; set; }
        public int? ExistingOwnerId { get; set; }
        public CreateOwnerRequest? NewOwner { get; set; }
    }

    public class UpdatePatientRequest
    {
        public string? Name { get; set; }
        public string? Species { get; set; }
        public string? Breed { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public int? WardId { get; set; }
    }

    public class TreatmentRequest
    {
        public string Diagnosis { get; set; } = default!;
        public string Treatment { get; set; } = default!;
        public string? Medication { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateTreatmentRequest : TreatmentRequest
    {
    }

    public class TreatmentDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string Diagnosis { get; set; } = default!;
        public string Treatment { get; set; } = default!;
        public string Medication { get; set; } = "";
        public string? Notes { get; set; }
        public string UpdatedByUserName { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }

    public class StatusHistoryDto
    {
        public int Id { get; set; }
        public string OldStatus { get; set; } = default!;
        public string NewStatus { get; set; } = default!;
        public string ChangedByUserName { get; set; } = default!;
        public DateTime ChangedAt { get; set; }
    }

    public class StatusChangeRequest
    {
        public string? NewClinicalStatus { get; set; }
        public string? NewAdmissionStatus { get; set; }
        public bool Confirm { get; set; } = false;
    }

    public class PatientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Species { get; set; } = default!;
        public string Breed { get; set; } = default!;
        public int Age { get; set; }
        public string Gender { get; set; } = default!;
        public OwnerDto Owner { get; set; } = default!;
        public WardDto Ward { get; set; } = default!;
        public string AdmissionStatus { get; set; } = default!;
        public string ClinicalStatus { get; set; } = default!;
        public DateTime AdmittedAt { get; set; }
        public DateTime? DischargedAt { get; set; }
        public List<TreatmentDto> Treatments { get; set; } = new();
        public List<StatusHistoryDto> StatusHistory { get; set; } = new();
        public List<NotificationDto> Notifications { get; set; } = new();
    }

    public class PatientSearchResultDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<PatientDto> Results { get; set; } = new();
    }
}
