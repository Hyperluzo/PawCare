using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawCare.Api.Data;
using PawCare.Api.DTOs;
using PawCare.Api.Models;

namespace PawCare.Api.Controllers
{
    [ApiController]
    [Route("api/wards")]
    [Authorize]
    public class WardsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public WardsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WardDto>>> GetAll()
        {
            var wards = await _db.Wards
                .OrderBy(w => w.Name)
                .Select(w => new WardDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Description = w.Description,
                    Capacity = w.Capacity,
                    PatientCount = _db.Patients.Count(p => p.WardId == w.Id && p.AdmissionStatus == "ADMITTED")
                })
                .ToListAsync();

            return Ok(wards);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<WardDto>> GetById(int id)
        {
            var ward = await _db.Wards
                .Where(w => w.Id == id)
                .Select(w => new WardDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Description = w.Description,
                    Capacity = w.Capacity,
                    PatientCount = _db.Patients.Count(p => p.WardId == w.Id && p.AdmissionStatus == "ADMITTED")
                })
                .FirstOrDefaultAsync();

            if (ward is null)
            {
                return NotFound(new { message = "Ward not found" });
            }

            return Ok(ward);
        }

        [HttpPost]
        public async Task<ActionResult<WardDto>> Create(CreateWardRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Ward name is required" });
            }

            if (request.Capacity <= 0)
            {
                return BadRequest(new { message = "Capacity must be greater than 0" });
            }

            var ward = new Ward
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim() ?? "",
                Capacity = request.Capacity
            };

            _db.Wards.Add(ward);
            await _db.SaveChangesAsync();

            var dto = new WardDto
            {
                Id = ward.Id,
                Name = ward.Name,
                Description = ward.Description,
                Capacity = ward.Capacity,
                PatientCount = 0
            };

            return CreatedAtAction(nameof(GetById), new { id = ward.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<WardDto>> Update(int id, UpdateWardRequest request)
        {
            var ward = await _db.Wards.FindAsync(id);
            if (ward is null)
            {
                return NotFound(new { message = "Ward not found" });
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                ward.Name = request.Name.Trim();
            }

            if (request.Description != null)
            {
                ward.Description = request.Description.Trim();
            }

            if (request.Capacity.HasValue)
            {
                if (request.Capacity.Value <= 0)
                {
                    return BadRequest(new { message = "Capacity must be greater than 0" });
                }
                ward.Capacity = request.Capacity.Value;
            }

            await _db.SaveChangesAsync();

            var patientCount = await _db.Patients.CountAsync(p => p.WardId == id && p.AdmissionStatus == "ADMITTED");

            return Ok(new WardDto
            {
                Id = ward.Id,
                Name = ward.Name,
                Description = ward.Description,
                Capacity = ward.Capacity,
                PatientCount = patientCount
            });
        }

        [HttpGet("{id}/patients")]
        public async Task<IActionResult> GetWardPatients(int id)
        {
            var wardExists = await _db.Wards.AnyAsync(w => w.Id == id);
            if (!wardExists)
            {
                return NotFound(new { message = "Ward not found" });
            }

            var patients = await _db.Patients
                .Where(p => p.WardId == id && p.AdmissionStatus == "ADMITTED")
                .Include(p => p.Owner)
                .Include(p => p.Ward)
                .ToListAsync();

            return Ok(patients);
        }
    }
}
