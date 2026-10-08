using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawCare.Api.Data;
using PawCare.Api.DTOs;
using PawCare.Api.Models;

namespace PawCare.Api.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "ADMIN")]
    public class AdminUsersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminUsersController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StaffUserDto>>> GetAll()
        {
            var users = await _db.Users
                .OrderBy(u => u.Id)
                .Select(u => new StaffUserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    AccountType = u.AccountType,
                    IsActive = u.IsActive
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StaffUserDto>> GetById(int id)
        {
            var user = await _db.Users
                .Where(u => u.Id == id)
                .Select(u => new StaffUserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    AccountType = u.AccountType,
                    IsActive = u.IsActive
                })
                .FirstOrDefaultAsync();

            if (user is null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<StaffUserDto>> Create(CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Email is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            {
                return BadRequest(new { message = "Password must be at least 6 characters" });
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var emailExists = await _db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
            if (emailExists)
            {
                return BadRequest(new { message = "A user with this email already exists" });
            }

            var role = (request.AccountType ?? "STAFF").Trim().ToUpperInvariant();
            if (role != "ADMIN" && role != "STAFF")
            {
                return BadRequest(new { message = "Role must be either ADMIN or STAFF" });
            }

            var user = new User
            {
                Name = request.Name.Trim(),
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                AccountType = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var dto = new StaffUserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                AccountType = user.AccountType,
                IsActive = user.IsActive
            };

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<StaffUserDto>> Update(int id, UpdateUserRequest request)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                user.Name = request.Name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var normalizedEmail = request.Email.Trim().ToLowerInvariant();
                var emailTaken = await _db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail && u.Id != id);
                if (emailTaken)
                {
                    return BadRequest(new { message = "Email is already taken by another user" });
                }
                user.Email = normalizedEmail;
            }

            if (!string.IsNullOrWhiteSpace(request.AccountType))
            {
                var role = request.AccountType.Trim().ToUpperInvariant();
                if (role != "ADMIN" && role != "STAFF")
                {
                    return BadRequest(new { message = "Role must be either ADMIN or STAFF" });
                }
                user.AccountType = role;
            }

            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new StaffUserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                AccountType = user.AccountType,
                IsActive = user.IsActive
            });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateUserStatusRequest request)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "New password must be at least 6 characters" });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Password reset successfully" });
        }
    }
}
