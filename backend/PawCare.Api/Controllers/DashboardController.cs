using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawCare.Api.Data;
using PawCare.Api.DTOs;
using PawCare.Api.Models;

namespace PawCare.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _db;

        public DashboardController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetStats()
        {
            var nowUtc = DateTime.UtcNow;
            var startOfToday = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, DateTimeKind.Utc);
            var startOfTomorrow = startOfToday.AddDays(1);
            var startOfMonth = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfNextMonth = startOfMonth.AddMonths(1);

            // Core Day 13 metrics
            var totalPatients = await _db.Patients.CountAsync();

            var underTreatment = await _db.Patients
                .CountAsync(p => p.ClinicalStatus == ClinicalStatuses.UnderTreatment);

            // TreatedToday: Patients discharged today (either DischargedAt is today, or status history record for DISCHARGED is today)
            var dischargedTodayHistoryIds = await _db.StatusHistories
                .Where(s => s.NewStatus == AdmissionStatuses.Discharged && s.ChangedAt >= startOfToday && s.ChangedAt < startOfTomorrow)
                .Select(s => s.PatientId)
                .Distinct()
                .ToListAsync();

            var treatedToday = await _db.Patients
                .CountAsync(p => p.AdmissionStatus == AdmissionStatuses.Discharged &&
                    ((p.DischargedAt != null && p.DischargedAt >= startOfToday && p.DischargedAt < startOfTomorrow) ||
                     dischargedTodayHistoryIds.Contains(p.Id)));

            // DeceasedMTD: Precision audit check from StatusHistories as recommended by roadmap + patient fallback
            var deceasedHistoryPatientIds = await _db.StatusHistories
                .Where(s => s.NewStatus == AdmissionStatuses.Deceased && s.ChangedAt >= startOfMonth && s.ChangedAt < startOfNextMonth)
                .Select(s => s.PatientId)
                .Distinct()
                .ToListAsync();

            var deceasedMtd = await _db.Patients
                .CountAsync(p => p.AdmissionStatus == AdmissionStatuses.Deceased &&
                    ((p.DischargedAt != null && p.DischargedAt >= startOfMonth && p.DischargedAt < startOfNextMonth) ||
                     deceasedHistoryPatientIds.Contains(p.Id) ||
                     (p.DischargedAt == null && p.CreatedAt >= startOfMonth && p.CreatedAt < startOfNextMonth)));

            // Operational status distribution
            var admittedPatients = await _db.Patients
                .CountAsync(p => p.AdmissionStatus == AdmissionStatuses.Admitted);

            var dischargedTotal = await _db.Patients
                .CountAsync(p => p.AdmissionStatus == AdmissionStatuses.Discharged);

            var criticalPatients = await _db.Patients
                .CountAsync(p => p.AdmissionStatus == AdmissionStatuses.Admitted && p.ClinicalStatus == ClinicalStatuses.Critical);

            var stablePatients = await _db.Patients
                .CountAsync(p => p.AdmissionStatus == AdmissionStatuses.Admitted && p.ClinicalStatus == ClinicalStatuses.Stable);

            var recoveringPatients = await _db.Patients
                .CountAsync(p => p.AdmissionStatus == AdmissionStatuses.Admitted && p.ClinicalStatus == ClinicalStatuses.Recovering);

            var underObservationPatients = await _db.Patients
                .CountAsync(p => p.AdmissionStatus == AdmissionStatuses.Admitted && p.ClinicalStatus == ClinicalStatuses.UnderObservation);

            // Wards & Bed occupancy
            var wards = await _db.Wards.AsNoTracking().ToListAsync();

            var wardOccupancies = await _db.Patients
                .AsNoTracking()
                .Where(p => p.AdmissionStatus == AdmissionStatuses.Admitted)
                .GroupBy(p => p.WardId)
                .Select(g => new { WardId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.WardId, x => x.Count);

            var wardSummaries = wards.Select(w =>
            {
                var count = wardOccupancies.TryGetValue(w.Id, out var c) ? c : 0;
                var available = Math.Max(0, w.Capacity - count);
                var percent = w.Capacity > 0 ? Math.Round((double)count / w.Capacity * 100.0, 1) : 0.0;
                return new DashboardWardSummaryDto
                {
                    WardId = w.Id,
                    Name = w.Name,
                    Capacity = w.Capacity,
                    CurrentOccupancy = count,
                    AvailableBeds = available,
                    OccupancyPercent = percent
                };
            }).OrderBy(w => w.Name).ToList();

            var totalCapacity = wards.Sum(w => w.Capacity);
            var occupiedBeds = wardOccupancies.Values.Sum();
            var occupancyRate = totalCapacity > 0 ? Math.Round((double)occupiedBeds / totalCapacity * 100.0, 1) : 0.0;

            // 5 most recent admitted patients
            var recentAdmissions = await _db.Patients
                .AsNoTracking()
                .Include(p => p.Ward)
                .Include(p => p.Owner)
                .OrderByDescending(p => p.AdmittedAt)
                .Take(5)
                .Select(p => new DashboardRecentPatientDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Species = p.Species,
                    Breed = p.Breed,
                    ClinicalStatus = p.ClinicalStatus,
                    AdmissionStatus = p.AdmissionStatus,
                    WardName = p.Ward != null ? p.Ward.Name : "Unassigned",
                    OwnerName = p.Owner != null ? p.Owner.Name : "Unknown",
                    AdmittedAt = p.AdmittedAt
                })
                .ToListAsync();

            var dto = new DashboardStatsDto
            {
                TotalPatients = totalPatients,
                UnderTreatment = underTreatment,
                TreatedToday = treatedToday,
                DeceasedMtd = deceasedMtd,
                AdmittedPatients = admittedPatients,
                DischargedTotal = dischargedTotal,
                CriticalPatients = criticalPatients,
                StablePatients = stablePatients,
                RecoveringPatients = recoveringPatients,
                UnderObservationPatients = underObservationPatients,
                TotalWards = wards.Count,
                TotalCapacity = totalCapacity,
                OccupiedBeds = occupiedBeds,
                OccupancyRate = occupancyRate,
                WardSummaries = wardSummaries,
                RecentAdmissions = recentAdmissions
            };

            return Ok(dto);
        }

        [HttpGet]
        public async Task<ActionResult<DashboardStatsDto>> Get() => await GetStats();
    }
}
