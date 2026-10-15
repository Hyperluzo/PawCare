namespace PawCare.Api.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalPatients { get; set; }
        public int UnderTreatment { get; set; }
        public int TreatedToday { get; set; }
        public int DeceasedMtd { get; set; }

        // Additional hospital operational metrics
        public int AdmittedPatients { get; set; }
        public int DischargedTotal { get; set; }
        public int CriticalPatients { get; set; }
        public int StablePatients { get; set; }
        public int RecoveringPatients { get; set; }
        public int UnderObservationPatients { get; set; }
        public int TotalWards { get; set; }
        public int TotalCapacity { get; set; }
        public int OccupiedBeds { get; set; }
        public double OccupancyRate { get; set; }
        public List<DashboardRecentPatientDto> RecentAdmissions { get; set; } = new();
        public List<DashboardWardSummaryDto> WardSummaries { get; set; } = new();
    }

    public class DashboardRecentPatientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public string ClinicalStatus { get; set; } = string.Empty;
        public string AdmissionStatus { get; set; } = string.Empty;
        public string WardName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public DateTime AdmittedAt { get; set; }
    }

    public class DashboardWardSummaryDto
    {
        public int WardId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int CurrentOccupancy { get; set; }
        public int AvailableBeds { get; set; }
        public double OccupancyPercent { get; set; }
    }
}
