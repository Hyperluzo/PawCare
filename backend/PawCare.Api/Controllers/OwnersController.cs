using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawCare.Api.Data;
using PawCare.Api.DTOs;
using PawCare.Api.Models;

namespace PawCare.Api.Controllers
{
    [ApiController]
    [Route("api/owners")]
    [Authorize]
    public class OwnersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public OwnersController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OwnerDto>>> GetAll([FromQuery] string? search = null)
        {
            var query = _db.Owners.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(o =>
                    EF.Functions.ILike(o.Name, $"%{term}%") ||
                    EF.Functions.ILike(o.Email, $"%{term}%") ||
                    EF.Functions.ILike(o.Phone, $"%{term}%"));
            }

            var owners = await query
                .OrderBy(o => o.Name)
                .Select(o => new OwnerDto
                {
                    Id = o.Id,
                    Name = o.Name,
                    Email = o.Email,
                    Phone = o.Phone,
                    Address = null
                })
                .ToListAsync();

            return Ok(owners);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OwnerDto>> GetById(int id)
        {
            var owner = await _db.Owners.FindAsync(id);
            if (owner is null)
            {
                return NotFound(new { message = "Owner not found" });
            }

            return Ok(new OwnerDto
            {
                Id = owner.Id,
                Name = owner.Name,
                Email = owner.Email,
                Phone = owner.Phone,
                Address = null
            });
        }

        [HttpPost]
        public async Task<ActionResult<OwnerDto>> Create(CreateOwnerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Owner name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Owner email is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Phone))
            {
                return BadRequest(new { message = "Owner phone is required" });
            }

            var owner = new Owner
            {
                Name = request.Name.Trim(),
                Email = request.Email.Trim(),
                Phone = request.Phone.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.Owners.Add(owner);
            await _db.SaveChangesAsync();

            var dto = new OwnerDto
            {
                Id = owner.Id,
                Name = owner.Name,
                Email = owner.Email,
                Phone = owner.Phone,
                Address = null
            };

            return CreatedAtAction(nameof(GetById), new { id = owner.Id }, dto);
        }
    }
}
