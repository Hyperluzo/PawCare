namespace PawCare.Api.Models
{
    public class Ward
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public int Capacity { get; set; }
    }
}