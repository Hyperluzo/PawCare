using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawCare.Api.Controllers;
using PawCare.Api.Data;
using PawCare.Api.DTOs;
using PawCare.Api.Models;
using Xunit;

namespace PawCare.Api.Tests
{
    public class DashboardControllerTests
    {
        private AppDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public void Controller_HasAuthorizeAttribute()
        {
            var type = typeof(DashboardController);
            var authorizeAttr = type.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(authorizeAttr);
        }

        [Fact]
        public async Task GetStats_EmptyDatabase_ReturnsAllZeros()
        {
            var db = CreateDbContext(Guid.NewGuid().ToString());
            var controller = new DashboardController(db);

            var actionResult = await controller.GetStats();
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var stats = Assert.IsType<DashboardStatsDto>(okResult.Value);

            Assert.Equal(0, stats.TotalPatients);
            Assert.Equal(0, stats.UnderTreatment);
            Assert.Equal(0, stats.TreatedToday);
            Assert.Equal(0, stats.DeceasedMtd);
            Assert.Equal(0, stats.TotalWards);
            Assert.Equal(0, stats.TotalCapacity);
            Assert.Equal(0, stats.OccupiedBeds);
            Assert.Empty(stats.WardSummaries);
            Assert.Empty(stats.RecentAdmissions);
        }

        [Fact]
        public async Task GetStats_CalculatesLivePatientCountsAccurately()
        {
            var db = CreateDbContext(Guid.NewGuid().ToString());
            var now = DateTime.UtcNow;

            var ward = new Ward { Id = 1, Name = "General Ward", Description = "General Ward Desc", Capacity = 10 };
            var owner = new Owner { Id = 1, Name = "Alice", Email = "alice@example.com", Phone = "12345" };
            db.Wards.Add(ward);
            db.Owners.Add(owner);

            // 1) Under treatment patient
            db.Patients.Add(new Patient
            {
                Id = 1,
                Name = "Max",
                Species = "Canine",
                Breed = "Retriever",
                Age = 3,
                Gender = "Male",
                OwnerId = 1,
                WardId = 1,
                AdmissionStatus = AdmissionStatuses.Admitted,
                ClinicalStatus = ClinicalStatuses.UnderTreatment,
                AdmittedAt = now.AddDays(-2)
            });

            // 2) Critical patient (also admitted)
            db.Patients.Add(new Patient
            {
                Id = 2,
                Name = "Bella",
                Species = "Feline",
                Breed = "Siamese",
                Age = 2,
                Gender = "Female",
                OwnerId = 1,
                WardId = 1,
                AdmissionStatus = AdmissionStatuses.Admitted,
                ClinicalStatus = ClinicalStatuses.Critical,
                AdmittedAt = now.AddDays(-1)
            });

            // 3) Discharged today (TreatedToday)
            db.Patients.Add(new Patient
            {
                Id = 3,
                Name = "Charlie",
                Species = "Canine",
                Breed = "Beagle",
                Age = 4,
                Gender = "Male",
                OwnerId = 1,
                WardId = 1,
                AdmissionStatus = AdmissionStatuses.Discharged,
                ClinicalStatus = ClinicalStatuses.Stable,
                AdmittedAt = now.AddDays(-3),
                DischargedAt = now
            });

            // 4) Discharged yesterday (should NOT be counted in TreatedToday)
            db.Patients.Add(new Patient
            {
                Id = 4,
                Name = "Daisy",
                Species = "Canine",
                Breed = "Poodle",
                Age = 5,
                Gender = "Female",
                OwnerId = 1,
                WardId = 1,
                AdmissionStatus = AdmissionStatuses.Discharged,
                ClinicalStatus = ClinicalStatuses.Recovering,
                AdmittedAt = now.AddDays(-5),
                DischargedAt = now.AddDays(-1)
            });

            // 5) Deceased this month (DeceasedMTD)
            db.Patients.Add(new Patient
            {
                Id = 5,
                Name = "Rocky",
                Species = "Feline",
                Breed = "Persian",
                Age = 8,
                Gender = "Male",
                OwnerId = 1,
                WardId = 1,
                AdmissionStatus = AdmissionStatuses.Deceased,
                ClinicalStatus = ClinicalStatuses.Critical,
                AdmittedAt = now.AddDays(-4),
                DischargedAt = now
            });

            await db.SaveChangesAsync();

            var controller = new DashboardController(db);
            var actionResult = await controller.GetStats();
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var stats = Assert.IsType<DashboardStatsDto>(okResult.Value);

            Assert.Equal(5, stats.TotalPatients);
            Assert.Equal(1, stats.UnderTreatment);
            Assert.Equal(1, stats.TreatedToday);
            Assert.Equal(1, stats.DeceasedMtd);
            Assert.Equal(2, stats.AdmittedPatients); // Max and Bella
            Assert.Equal(2, stats.DischargedTotal); // Charlie and Daisy
            Assert.Equal(1, stats.CriticalPatients);
        }

        [Fact]
        public async Task GetStats_DeceasedMTD_DetectsStatusHistoryAuditTrail()
        {
            var db = CreateDbContext(Guid.NewGuid().ToString());
            var now = DateTime.UtcNow;

            var ward = new Ward { Id = 1, Name = "General Ward", Description = "General Ward Desc", Capacity = 10 };
            var owner = new Owner { Id = 1, Name = "Bob", Email = "bob@example.com", Phone = "12345" };
            var user = new User { Id = 1, Name = "Vet", Email = "vet@pawcare.local", PasswordHash = "hash", AccountType = "STAFF" };
            db.Wards.Add(ward);
            db.Owners.Add(owner);
            db.Users.Add(user);

            var deceasedPatient = new Patient
            {
                Id = 10,
                Name = "Milo",
                Species = "Canine",
                Breed = "Husky",
                Age = 6,
                Gender = "Male",
                OwnerId = 1,
                WardId = 1,
                AdmissionStatus = AdmissionStatuses.Deceased,
                ClinicalStatus = ClinicalStatuses.Critical,
                AdmittedAt = now.AddDays(-2),
                DischargedAt = null // DischargedAt is null, but StatusHistory records it
            };
            db.Patients.Add(deceasedPatient);

            db.StatusHistories.Add(new StatusHistory
            {
                PatientId = 10,
                OldStatus = AdmissionStatuses.Admitted,
                NewStatus = AdmissionStatuses.Deceased,
                ChangedByUserId = 1,
                ChangedAt = now
            });

            // Another patient deceased 2 months ago (must not be counted in MTD)
            var oldDeceasedPatient = new Patient
            {
                Id = 11,
                Name = "Luna",
                Species = "Feline",
                Breed = "Tabby",
                Age = 10,
                Gender = "Female",
                OwnerId = 1,
                WardId = 1,
                AdmissionStatus = AdmissionStatuses.Deceased,
                ClinicalStatus = ClinicalStatuses.Critical,
                AdmittedAt = now.AddMonths(-3),
                DischargedAt = now.AddMonths(-2),
                CreatedAt = now.AddMonths(-3)
            };
            db.Patients.Add(oldDeceasedPatient);

            db.StatusHistories.Add(new StatusHistory
            {
                PatientId = 11,
                OldStatus = AdmissionStatuses.Admitted,
                NewStatus = AdmissionStatuses.Deceased,
                ChangedByUserId = 1,
                ChangedAt = now.AddMonths(-2)
            });

            await db.SaveChangesAsync();

            var controller = new DashboardController(db);
            var actionResult = await controller.GetStats();
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var stats = Assert.IsType<DashboardStatsDto>(okResult.Value);

            Assert.Equal(2, stats.TotalPatients);
            Assert.Equal(1, stats.DeceasedMtd); // only Milo is MTD
        }

        [Fact]
        public async Task GetStats_ComputesWardOccupancyAndCapacityCorrectly()
        {
            var db = CreateDbContext(Guid.NewGuid().ToString());
            var now = DateTime.UtcNow;

            var wardA = new Ward { Id = 1, Name = "General Ward", Description = "General Ward Desc", Capacity = 10 };
            var wardB = new Ward { Id = 2, Name = "ICU", Description = "ICU Desc", Capacity = 4 };
            db.Wards.AddRange(wardA, wardB);

            var owner = new Owner { Id = 1, Name = "Emma", Email = "emma@example.com", Phone = "12345" };
            db.Owners.Add(owner);

            // 3 patients in General Ward, 2 admitted, 1 discharged
            db.Patients.Add(new Patient
            {
                Id = 1,
                Name = "Patient 1",
                Species = "Canine",
                Breed = "Collie",
                Age = 2,
                Gender = "Male",
                OwnerId = 1,
                WardId = 1,
                AdmissionStatus = AdmissionStatuses.Admitted,
                ClinicalStatus = ClinicalStatuses.UnderTreatment,
                AdmittedAt = now
            });
            db.Patients.Add(new Patient
            {
                Id = 2,
                Name = "Patient 2",
                Species = "Canine",
                Breed = "Bulldog",
                Age = 3,
                Gender = "Female",
                OwnerId = 1,
                WardId = 1,
                AdmissionStatus = AdmissionStatuses.Admitted,
                ClinicalStatus = ClinicalStatuses.Stable,
                AdmittedAt = now
            });
            db.Patients.Add(new Patient
            {
                Id = 3,
                Name = "Patient 3",
                Species = "Canine",
                Breed = "Pug",
                Age = 1,
                Gender = "Male",
                OwnerId = 1,
                WardId = 1,
                AdmissionStatus = AdmissionStatuses.Discharged, // Discharged doesn't occupy bed
                ClinicalStatus = ClinicalStatuses.Stable,
                AdmittedAt = now.AddDays(-1),
                DischargedAt = now
            });

            // 1 patient in ICU, admitted
            db.Patients.Add(new Patient
            {
                Id = 4,
                Name = "Patient 4",
                Species = "Feline",
                Breed = "Sphynx",
                Age = 5,
                Gender = "Female",
                OwnerId = 1,
                WardId = 2,
                AdmissionStatus = AdmissionStatuses.Admitted,
                ClinicalStatus = ClinicalStatuses.Critical,
                AdmittedAt = now
            });

            await db.SaveChangesAsync();

            var controller = new DashboardController(db);
            var actionResult = await controller.GetStats();
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var stats = Assert.IsType<DashboardStatsDto>(okResult.Value);

            Assert.Equal(2, stats.TotalWards);
            Assert.Equal(14, stats.TotalCapacity); // 10 + 4
            Assert.Equal(3, stats.OccupiedBeds); // 2 in ward 1 + 1 in ward 2
            Assert.Equal(21.4, stats.OccupancyRate); // 3 / 14 * 100% = 21.4%

            var genWardSummary = stats.WardSummaries.First(w => w.WardId == 1);
            Assert.Equal(2, genWardSummary.CurrentOccupancy);
            Assert.Equal(8, genWardSummary.AvailableBeds);
            Assert.Equal(20.0, genWardSummary.OccupancyPercent);

            var icuWardSummary = stats.WardSummaries.First(w => w.WardId == 2);
            Assert.Equal(1, icuWardSummary.CurrentOccupancy);
            Assert.Equal(3, icuWardSummary.AvailableBeds);
            Assert.Equal(25.0, icuWardSummary.OccupancyPercent);
        }

        [Fact]
        public async Task GetStats_LimitsRecentAdmissionsToFiveOrderedNewestFirst()
        {
            var db = CreateDbContext(Guid.NewGuid().ToString());
            var baseTime = DateTime.UtcNow;

            var ward = new Ward { Id = 1, Name = "General Ward", Description = "General Ward Desc", Capacity = 20 };
            var owner = new Owner { Id = 1, Name = "Owner", Email = "owner@example.com", Phone = "12345" };
            db.Wards.Add(ward);
            db.Owners.Add(owner);

            for (int i = 1; i <= 8; i++)
            {
                db.Patients.Add(new Patient
                {
                    Id = i,
                    Name = $"Patient #{i}",
                    Species = "Canine",
                    Breed = "Mix",
                    Age = i,
                    Gender = "Male",
                    OwnerId = 1,
                    WardId = 1,
                    AdmissionStatus = AdmissionStatuses.Admitted,
                    ClinicalStatus = ClinicalStatuses.UnderTreatment,
                    AdmittedAt = baseTime.AddHours(i) // Patient 8 is newest
                });
            }

            await db.SaveChangesAsync();

            var controller = new DashboardController(db);
            var actionResult = await controller.GetStats();
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var stats = Assert.IsType<DashboardStatsDto>(okResult.Value);

            Assert.Equal(8, stats.TotalPatients);
            Assert.Equal(5, stats.RecentAdmissions.Count);
            Assert.Equal(8, stats.RecentAdmissions[0].Id);
            Assert.Equal(7, stats.RecentAdmissions[1].Id);
            Assert.Equal(6, stats.RecentAdmissions[2].Id);
            Assert.Equal(5, stats.RecentAdmissions[3].Id);
            Assert.Equal(4, stats.RecentAdmissions[4].Id);
        }
    }
}
