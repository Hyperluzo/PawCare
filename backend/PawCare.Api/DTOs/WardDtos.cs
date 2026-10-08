namespace PawCare.Api.DTOs
{
    public class WardDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public int Capacity { get; set; }
        public int PatientCount { get; set; }
    }

    public class CreateWardRequest
    {
        public string Name { get; set; } = default!;
        public string Description { get; set; } = "";
        public int Capacity { get; set; }
    }

    public class UpdateWardRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? Capacity { get; set; }
    }
}
