namespace PawCare.Api.Models
{
    public static class AdmissionStatuses
    {
        public const string Admitted = "ADMITTED";
        public const string Discharged = "DISCHARGED";
        public const string Deceased = "DECEASED";

        public static readonly string[] All = [Admitted, Discharged, Deceased];

        public static bool IsValid(string? status) =>
            !string.IsNullOrWhiteSpace(status) &&
            All.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase);

        public static string? Normalize(string? status) =>
            All.FirstOrDefault(s => s.Equals(status?.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static class ClinicalStatuses
    {
        public const string UnderTreatment = "UNDER_TREATMENT";
        public const string Stable = "STABLE";
        public const string Critical = "CRITICAL";
        public const string Recovering = "RECOVERING";
        public const string UnderObservation = "UNDER_OBSERVATION";

        public static readonly string[] All =
        [
            UnderTreatment,
            Stable,
            Critical,
            Recovering,
            UnderObservation
        ];

        public static bool IsValid(string? status) =>
            !string.IsNullOrWhiteSpace(status) &&
            All.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase);

        public static string? Normalize(string? status) =>
            All.FirstOrDefault(s => s.Equals(status?.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
