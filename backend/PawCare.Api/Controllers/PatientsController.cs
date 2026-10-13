using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PawCare.Api.Data;
using PawCare.Api.DTOs;
using PawCare.Api.Models;
using PawCare.Api.Services;
using System.Security.Claims;

namespace PawCare.Api.Controllers
{
    [ApiController]
    [Route("api/patients")]
    [Authorize]
    public class PatientsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly INotificationService? _notificationService;
        private readonly ILogger<PatientsController> _logger;

        public PatientsController(
            AppDbContext db,
            INotificationService? notificationService = null,
            ILogger<PatientsController>? logger = null)
        {
            _db = db;
            _notificationService = notificationService;
            _logger = logger ?? NullLogger<PatientsController>.Instance;
        }

        [HttpGet]
        public async Task<ActionResult<PatientSearchResultDto>> GetAll(
            [FromQuery] string? name = null,
            [FromQuery] string? species = null,
            [FromQuery] string? breed = null,
            [FromQuery] string? owner = null,
            [FromQuery] string? status = null,
            [FromQuery] string? clinicalStatus = null,
            [FromQuery] string? admissionStatus = null,
            [FromQuery] int? wardId = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string? sortOrder = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _db.Patients
                .Include(p => p.Owner)
                .Include(p => p.Ward)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p =>
                    EF.Functions.ILike(p.Name, $"%{term}%") ||
                    EF.Functions.ILike(p.Species, $"%{term}%") ||
                    EF.Functions.ILike(p.Breed, $"%{term}%") ||
                    EF.Functions.ILike(p.Owner.Name, $"%{term}%") ||
                    EF.Functions.ILike(p.Owner.Phone, $"%{term}%") ||
                    EF.Functions.ILike(p.Ward.Name, $"%{term}%"));
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                var trimmedName = name.Trim();
                query = query.Where(p => EF.Functions.ILike(p.Name, $"%{trimmedName}%"));
            }

            if (!string.IsNullOrWhiteSpace(species))
            {
                var trimmedSpecies = species.Trim();
                query = query.Where(p => EF.Functions.ILike(p.Species, $"%{trimmedSpecies}%"));
            }

            if (!string.IsNullOrWhiteSpace(breed))
            {
                var trimmedBreed = breed.Trim();
                query = query.Where(p => EF.Functions.ILike(p.Breed, $"%{trimmedBreed}%"));
            }

            if (!string.IsNullOrWhiteSpace(owner))
            {
                var trimmedOwner = owner.Trim();
                query = query.Where(p =>
                    EF.Functions.ILike(p.Owner.Name, $"%{trimmedOwner}%") ||
                    EF.Functions.ILike(p.Owner.Phone, $"%{trimmedOwner}%") ||
                    EF.Functions.ILike(p.Owner.Email, $"%{trimmedOwner}%"));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var trimmedStatus = status.Trim();
                query = query.Where(p => p.ClinicalStatus == trimmedStatus || p.AdmissionStatus == trimmedStatus);
            }

            if (!string.IsNullOrWhiteSpace(clinicalStatus))
            {
                var trimmedClinicalStatus = clinicalStatus.Trim();
                query = query.Where(p => p.ClinicalStatus == trimmedClinicalStatus);
            }

            if (!string.IsNullOrWhiteSpace(admissionStatus))
            {
                var trimmedAdmissionStatus = admissionStatus.Trim();
                query = query.Where(p => p.AdmissionStatus == trimmedAdmissionStatus);
            }

            if (wardId.HasValue && wardId.Value > 0)
            {
                query = query.Where(p => p.WardId == wardId.Value);
            }

            var total = await query.CountAsync();

            var safePage = Math.Max(1, page);
            var safePageSize = Math.Clamp(pageSize, 1, 100);

            var sort = (sortBy ?? "createdAt").Trim().ToLowerInvariant();
            var order = (sortOrder ?? "desc").Trim().ToLowerInvariant();

            query = (sort, order) switch
            {
                ("name", "asc") => query.OrderBy(p => p.Name),
                ("name", "desc") => query.OrderByDescending(p => p.Name),
                ("species", "asc") => query.OrderBy(p => p.Species).ThenBy(p => p.Breed),
                ("species", "desc") => query.OrderByDescending(p => p.Species).ThenBy(p => p.Breed),
                ("admittedat", "asc") => query.OrderBy(p => p.AdmittedAt),
                ("admittedat", "desc") => query.OrderByDescending(p => p.AdmittedAt),
                ("clinicalstatus", "asc") => query.OrderBy(p => p.ClinicalStatus),
                ("clinicalstatus", "desc") => query.OrderByDescending(p => p.ClinicalStatus),
                ("admissionstatus", "asc") => query.OrderBy(p => p.AdmissionStatus),
                ("admissionstatus", "desc") => query.OrderByDescending(p => p.AdmissionStatus),
                ("createdat", "asc") => query.OrderBy(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var patients = await query
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToListAsync();

            var wardIds = patients.Select(p => p.WardId).Distinct().ToList();
            var wardOccupancies = await _db.Patients
                .Where(p => wardIds.Contains(p.WardId) && p.AdmissionStatus == "ADMITTED")
                .GroupBy(p => p.WardId)
                .Select(g => new { WardId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.WardId, x => x.Count);

            var results = patients.Select(p => MapToDto(
                p,
                p.Owner,
                p.Ward,
                wardOccupancies.GetValueOrDefault(p.WardId, 0)
            )).ToList();

            return Ok(new PatientSearchResultDto
            {
                Total = total,
                Page = safePage,
                PageSize = safePageSize,
                Results = results
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PatientDto>> GetById(int id)
        {
            var patient = await _db.Patients
                .Include(p => p.Owner)
                .Include(p => p.Ward)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient is null)
            {
                return NotFound(new { message = "Patient not found" });
            }

            var wardOccupancy = await _db.Patients
                .CountAsync(p => p.WardId == patient.WardId && p.AdmissionStatus == "ADMITTED");

            var treatments = await _db.Treatments
                .Where(t => t.PatientId == id)
                .Include(t => t.AddedByUser)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TreatmentDto
                {
                    Id = t.Id,
                    PatientId = t.PatientId,
                    Diagnosis = !string.IsNullOrEmpty(t.Diagnosis) ? t.Diagnosis : t.Description,
                    Treatment = !string.IsNullOrEmpty(t.TreatmentPlan) ? t.TreatmentPlan : t.Description,
                    Medication = t.Medication ?? string.Empty,
                    Notes = t.Notes,
                    UpdatedByUserName = t.AddedByUser != null ? t.AddedByUser.Name : "Staff",
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            var statusHistory = await _db.StatusHistories
                .Where(s => s.PatientId == id)
                .Include(s => s.ChangedByUser)
                .OrderByDescending(s => s.ChangedAt)
                .Select(s => new StatusHistoryDto
                {
                    Id = s.Id,
                    OldStatus = s.OldStatus,
                    NewStatus = s.NewStatus,
                    ChangedByUserName = s.ChangedByUser != null ? s.ChangedByUser.Name : "Staff",
                    ChangedAt = s.ChangedAt
                })
                .ToListAsync();

            var notifications = await _db.Notifications
                .Where(n => n.PatientId == id)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    PatientId = n.PatientId,
                    PatientName = patient.Name,
                    OwnerId = n.OwnerId,
                    OwnerName = patient.Owner.Name,
                    NotificationType = n.NotificationType,
                    RecipientEmail = n.RecipientEmail,
                    DeliveryStatus = n.DeliveryStatus,
                    ErrorMessage = n.ErrorMessage,
                    SentAt = n.SentAt,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(MapToDto(patient, patient.Owner, patient.Ward, wardOccupancy, treatments, statusHistory, notifications));
        }

        [HttpPost]
        public async Task<ActionResult<PatientDto>> Admit(AdmitPatientRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PatientName))
            {
                return BadRequest(new { message = "Patient name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Species))
            {
                return BadRequest(new { message = "Species is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Breed))
            {
                return BadRequest(new { message = "Breed is required" });
            }

            if (request.Age < 0)
            {
                return BadRequest(new { message = "Age cannot be negative" });
            }

            if (string.IsNullOrWhiteSpace(request.Gender))
            {
                return BadRequest(new { message = "Gender is required" });
            }

            var ward = await _db.Wards.FindAsync(request.WardId);
            if (ward is null)
            {
                return BadRequest(new { message = "Ward not found" });
            }

            var currentOccupancy = await _db.Patients
                .CountAsync(p => p.WardId == ward.Id && p.AdmissionStatus == "ADMITTED");

            if (currentOccupancy >= ward.Capacity)
            {
                return BadRequest(new { message = $"Ward '{ward.Name}' is at full capacity ({ward.Capacity}/{ward.Capacity})" });
            }

            Owner? owner;
            if (request.ExistingOwnerId.HasValue && request.ExistingOwnerId.Value > 0)
            {
                owner = await _db.Owners.FindAsync(request.ExistingOwnerId.Value);
                if (owner is null)
                {
                    return BadRequest(new { message = "Specified existing owner not found" });
                }
            }
            else if (request.NewOwner is not null)
            {
                if (string.IsNullOrWhiteSpace(request.NewOwner.Name))
                {
                    return BadRequest(new { message = "Owner name is required" });
                }

                if (string.IsNullOrWhiteSpace(request.NewOwner.Email))
                {
                    return BadRequest(new { message = "Owner email is required" });
                }

                if (string.IsNullOrWhiteSpace(request.NewOwner.Phone))
                {
                    return BadRequest(new { message = "Owner phone is required" });
                }

                owner = new Owner
                {
                    Name = request.NewOwner.Name.Trim(),
                    Email = request.NewOwner.Email.Trim(),
                    Phone = request.NewOwner.Phone.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _db.Owners.Add(owner);
                await _db.SaveChangesAsync();
            }
            else
            {
                return BadRequest(new { message = "Either an existing owner ID or new owner details must be provided" });
            }

            var patient = new Patient
            {
                Name = request.PatientName.Trim(),
                Species = request.Species.Trim(),
                Breed = request.Breed.Trim(),
                Age = request.Age,
                Gender = request.Gender.Trim(),
                OwnerId = owner.Id,
                Owner = owner,
                WardId = ward.Id,
                Ward = ward,
                AdmissionStatus = "ADMITTED",
                ClinicalStatus = "UNDER_TREATMENT",
                AdmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var staffUserId))
            {
                _db.StatusHistories.Add(new StatusHistory
                {
                    PatientId = patient.Id,
                    OldStatus = "NONE",
                    NewStatus = AdmissionStatuses.Admitted,
                    ChangedByUserId = staffUserId,
                    ChangedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            var dto = MapToDto(patient, owner, ward, currentOccupancy + 1);

            return CreatedAtAction(nameof(GetById), new { id = patient.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<PatientDto>> Update(int id, UpdatePatientRequest request)
        {
            var patient = await _db.Patients
                .Include(p => p.Owner)
                .Include(p => p.Ward)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient is null)
            {
                return NotFound(new { message = "Patient not found" });
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                patient.Name = request.Name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Species))
            {
                patient.Species = request.Species.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Breed))
            {
                patient.Breed = request.Breed.Trim();
            }

            if (request.Age.HasValue)
            {
                if (request.Age.Value < 0)
                {
                    return BadRequest(new { message = "Age cannot be negative" });
                }
                patient.Age = request.Age.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Gender))
            {
                patient.Gender = request.Gender.Trim();
            }

            if (request.WardId.HasValue && request.WardId.Value != patient.WardId)
            {
                var newWard = await _db.Wards.FindAsync(request.WardId.Value);
                if (newWard is null)
                {
                    return BadRequest(new { message = "Target ward not found" });
                }

                if (patient.AdmissionStatus == "ADMITTED")
                {
                    var newWardOccupancy = await _db.Patients
                        .CountAsync(p => p.WardId == newWard.Id && p.AdmissionStatus == "ADMITTED");

                    if (newWardOccupancy >= newWard.Capacity)
                    {
                        return BadRequest(new { message = $"Target ward '{newWard.Name}' is at full capacity ({newWard.Capacity}/{newWard.Capacity})" });
                    }
                }

                patient.WardId = newWard.Id;
                patient.Ward = newWard;
            }

            await _db.SaveChangesAsync();

            var wardOccupancy = await _db.Patients
                .CountAsync(p => p.WardId == patient.WardId && p.AdmissionStatus == "ADMITTED");

            return Ok(MapToDto(patient, patient.Owner, patient.Ward, wardOccupancy));
        }

        [HttpGet("{id}/treatments")]
        public async Task<ActionResult<List<TreatmentDto>>> GetTreatments(int id)
        {
            var patientExists = await _db.Patients.AnyAsync(p => p.Id == id);
            if (!patientExists)
            {
                return NotFound(new { message = "Patient not found" });
            }

            var treatments = await _db.Treatments
                .Where(t => t.PatientId == id)
                .Include(t => t.AddedByUser)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TreatmentDto
                {
                    Id = t.Id,
                    PatientId = t.PatientId,
                    Diagnosis = !string.IsNullOrEmpty(t.Diagnosis) ? t.Diagnosis : t.Description,
                    Treatment = !string.IsNullOrEmpty(t.TreatmentPlan) ? t.TreatmentPlan : t.Description,
                    Medication = t.Medication ?? string.Empty,
                    Notes = t.Notes,
                    UpdatedByUserName = t.AddedByUser != null ? t.AddedByUser.Name : "Staff",
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(treatments);
        }

        [HttpPost("{id}/treatments")]
        public async Task<ActionResult<TreatmentDto>> AddTreatment(int id, TreatmentRequest request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "User identity not found in token" });
            }

            var user = await _db.Users.FindAsync(userId);
            if (user is null || !user.IsActive)
            {
                return Unauthorized(new { message = "User account not found or inactive" });
            }

            var patient = await _db.Patients.FindAsync(id);
            if (patient is null)
            {
                return NotFound(new { message = "Patient not found" });
            }

            if (string.IsNullOrWhiteSpace(request.Diagnosis))
            {
                return BadRequest(new { message = "Diagnosis is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Treatment))
            {
                return BadRequest(new { message = "Treatment description is required" });
            }

            var treatment = new Treatment
            {
                PatientId = patient.Id,
                Diagnosis = request.Diagnosis.Trim(),
                TreatmentPlan = request.Treatment.Trim(),
                Medication = request.Medication?.Trim() ?? string.Empty,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                Description = request.Treatment.Trim(),
                AddedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Treatments.Add(treatment);
            await _db.SaveChangesAsync();

            var dto = new TreatmentDto
            {
                Id = treatment.Id,
                PatientId = treatment.PatientId,
                Diagnosis = treatment.Diagnosis,
                Treatment = treatment.TreatmentPlan,
                Medication = treatment.Medication,
                Notes = treatment.Notes,
                UpdatedByUserName = user.Name,
                CreatedAt = treatment.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = patient.Id }, dto);
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult<PatientDto>> ChangeStatus(int id, StatusChangeRequest request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "User identity not found in token" });
            }

            var user = await _db.Users.FindAsync(userId);
            if (user is null || !user.IsActive)
            {
                return Unauthorized(new { message = "User account not found or inactive" });
            }

            var patient = await _db.Patients
                .Include(p => p.Owner)
                .Include(p => p.Ward)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient is null)
            {
                return NotFound(new { message = "Patient not found" });
            }

            if (string.IsNullOrWhiteSpace(request.NewClinicalStatus) && string.IsNullOrWhiteSpace(request.NewAdmissionStatus))
            {
                return BadRequest(new { message = "At least one of NewClinicalStatus or NewAdmissionStatus must be provided." });
            }

            string? normalizedClinical = null;
            if (!string.IsNullOrWhiteSpace(request.NewClinicalStatus))
            {
                normalizedClinical = ClinicalStatuses.Normalize(request.NewClinicalStatus);
                if (normalizedClinical is null)
                {
                    return BadRequest(new { message = $"Invalid clinical status '{request.NewClinicalStatus}'. Allowed values are: {string.Join(", ", ClinicalStatuses.All)}" });
                }
            }

            string? normalizedAdmission = null;
            if (!string.IsNullOrWhiteSpace(request.NewAdmissionStatus))
            {
                normalizedAdmission = AdmissionStatuses.Normalize(request.NewAdmissionStatus);
                if (normalizedAdmission is null)
                {
                    return BadRequest(new { message = $"Invalid admission status '{request.NewAdmissionStatus}'. Allowed values are: {string.Join(", ", AdmissionStatuses.All)}" });
                }

                if (normalizedAdmission == AdmissionStatuses.Deceased && !request.Confirm)
                {
                    return BadRequest(new { message = "DECEASED requires explicit confirmation." });
                }
            }

            var oldClinicalStatus = patient.ClinicalStatus;
            var oldAdmissionStatus = patient.AdmissionStatus;
            var now = DateTime.UtcNow;
            var historyEntries = new List<StatusHistory>();

            if (normalizedClinical != null)
            {
                patient.ClinicalStatus = normalizedClinical;
                if (!normalizedClinical.Equals(oldClinicalStatus, StringComparison.OrdinalIgnoreCase))
                {
                    historyEntries.Add(new StatusHistory
                    {
                        PatientId = patient.Id,
                        OldStatus = oldClinicalStatus,
                        NewStatus = normalizedClinical,
                        ChangedByUserId = userId,
                        ChangedAt = now
                    });
                }
            }

            if (normalizedAdmission != null)
            {
                patient.AdmissionStatus = normalizedAdmission;
                if (normalizedAdmission == AdmissionStatuses.Discharged || normalizedAdmission == AdmissionStatuses.Deceased)
                {
                    patient.DischargedAt = now;
                }
                else if (normalizedAdmission == AdmissionStatuses.Admitted)
                {
                    patient.DischargedAt = null;
                }

                if (!normalizedAdmission.Equals(oldAdmissionStatus, StringComparison.OrdinalIgnoreCase))
                {
                    historyEntries.Add(new StatusHistory
                    {
                        PatientId = patient.Id,
                        OldStatus = oldAdmissionStatus,
                        NewStatus = normalizedAdmission,
                        ChangedByUserId = userId,
                        ChangedAt = now
                    });
                }
            }

            // If neither value differed from previous state but user explicitly requested status update
            if (historyEntries.Count == 0)
            {
                historyEntries.Add(new StatusHistory
                {
                    PatientId = patient.Id,
                    OldStatus = oldClinicalStatus,
                    NewStatus = normalizedClinical ?? oldClinicalStatus,
                    ChangedByUserId = userId,
                    ChangedAt = now
                });
            }

            _db.StatusHistories.AddRange(historyEntries);
            await _db.SaveChangesAsync();

            // Day 12: Trigger SES status notification email decoupled from status commit
            string? notificationType = null;
            if (normalizedAdmission == AdmissionStatuses.Deceased)
            {
                notificationType = "DECEASED";
            }
            else if (normalizedAdmission == AdmissionStatuses.Discharged)
            {
                notificationType = "DISCHARGED";
            }
            else if (normalizedClinical != null)
            {
                notificationType = normalizedClinical;
            }

            if (!string.IsNullOrEmpty(notificationType) && _notificationService != null)
            {
                try
                {
                    await _notificationService.SendStatusEmailAsync(patient, notificationType);
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to send status email for patient {PatientId}", patient.Id);
                }
            }

            var wardOccupancy = await _db.Patients
                .CountAsync(p => p.WardId == patient.WardId && p.AdmissionStatus == AdmissionStatuses.Admitted);

            var treatments = await _db.Treatments
                .Where(t => t.PatientId == id)
                .Include(t => t.AddedByUser)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TreatmentDto
                {
                    Id = t.Id,
                    PatientId = t.PatientId,
                    Diagnosis = !string.IsNullOrEmpty(t.Diagnosis) ? t.Diagnosis : t.Description,
                    Treatment = !string.IsNullOrEmpty(t.TreatmentPlan) ? t.TreatmentPlan : t.Description,
                    Medication = t.Medication ?? string.Empty,
                    Notes = t.Notes,
                    UpdatedByUserName = t.AddedByUser != null ? t.AddedByUser.Name : "Staff",
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            var statusHistory = await _db.StatusHistories
                .Where(s => s.PatientId == id)
                .Include(s => s.ChangedByUser)
                .OrderByDescending(s => s.ChangedAt)
                .Select(s => new StatusHistoryDto
                {
                    Id = s.Id,
                    OldStatus = s.OldStatus,
                    NewStatus = s.NewStatus,
                    ChangedByUserName = s.ChangedByUser != null ? s.ChangedByUser.Name : "Staff",
                    ChangedAt = s.ChangedAt
                })
                .ToListAsync();

            var notifications = await _db.Notifications
                .Where(n => n.PatientId == id)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    PatientId = n.PatientId,
                    PatientName = patient.Name,
                    OwnerId = n.OwnerId,
                    OwnerName = patient.Owner.Name,
                    NotificationType = n.NotificationType,
                    RecipientEmail = n.RecipientEmail,
                    DeliveryStatus = n.DeliveryStatus,
                    ErrorMessage = n.ErrorMessage,
                    SentAt = n.SentAt,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(MapToDto(patient, patient.Owner, patient.Ward, wardOccupancy, treatments, statusHistory, notifications));
        }

        [HttpGet("{id}/status-history")]
        public async Task<ActionResult<List<StatusHistoryDto>>> GetStatusHistory(int id)
        {
            var patientExists = await _db.Patients.AnyAsync(p => p.Id == id);
            if (!patientExists)
            {
                return NotFound(new { message = "Patient not found" });
            }

            var history = await _db.StatusHistories
                .Where(s => s.PatientId == id)
                .Include(s => s.ChangedByUser)
                .OrderByDescending(s => s.ChangedAt)
                .Select(s => new StatusHistoryDto
                {
                    Id = s.Id,
                    OldStatus = s.OldStatus,
                    NewStatus = s.NewStatus,
                    ChangedByUserName = s.ChangedByUser != null ? s.ChangedByUser.Name : "Staff",
                    ChangedAt = s.ChangedAt
                })
                .ToListAsync();

            return Ok(history);
        }

        [HttpGet("{id}/notifications")]
        public async Task<ActionResult<List<NotificationDto>>> GetNotifications(int id)
        {
            var patient = await _db.Patients.Include(p => p.Owner).FirstOrDefaultAsync(p => p.Id == id);
            if (patient is null)
            {
                return NotFound(new { message = "Patient not found" });
            }

            var notifications = await _db.Notifications
                .Where(n => n.PatientId == id)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    PatientId = n.PatientId,
                    PatientName = patient.Name,
                    OwnerId = n.OwnerId,
                    OwnerName = patient.Owner.Name,
                    NotificationType = n.NotificationType,
                    RecipientEmail = n.RecipientEmail,
                    DeliveryStatus = n.DeliveryStatus,
                    ErrorMessage = n.ErrorMessage,
                    SentAt = n.SentAt,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(notifications);
        }

        private static PatientDto MapToDto(
            Patient patient,
            Owner owner,
            Ward ward,
            int wardOccupancy,
            List<TreatmentDto>? treatments = null,
            List<StatusHistoryDto>? statusHistory = null,
            List<NotificationDto>? notifications = null)
        {
            return new PatientDto
            {
                Id = patient.Id,
                Name = patient.Name,
                Species = patient.Species,
                Breed = patient.Breed,
                Age = patient.Age,
                Gender = patient.Gender,
                Owner = new OwnerDto
                {
                    Id = owner.Id,
                    Name = owner.Name,
                    Email = owner.Email,
                    Phone = owner.Phone,
                    Address = null
                },
                Ward = new WardDto
                {
                    Id = ward.Id,
                    Name = ward.Name,
                    Description = ward.Description,
                    Capacity = ward.Capacity,
                    PatientCount = wardOccupancy
                },
                AdmissionStatus = patient.AdmissionStatus,
                ClinicalStatus = patient.ClinicalStatus,
                AdmittedAt = patient.AdmittedAt,
                DischargedAt = patient.DischargedAt,
                Treatments = treatments ?? new List<TreatmentDto>(),
                StatusHistory = statusHistory ?? new List<StatusHistoryDto>(),
                Notifications = notifications ?? new List<NotificationDto>()
            };
        }
    }
}
