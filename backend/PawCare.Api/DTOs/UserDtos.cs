namespace PawCare.Api.DTOs
{
    public class StaffUserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string AccountType { get; set; } = default!;
        public bool IsActive { get; set; }
    }

    public class CreateUserRequest
    {
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string AccountType { get; set; } = "STAFF";
    }

    public class UpdateUserRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? AccountType { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UpdateUserStatusRequest
    {
        public bool IsActive { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = default!;
    }
}
