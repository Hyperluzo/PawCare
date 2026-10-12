using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawCare.Api.Controllers;
using PawCare.Api.Data;
using PawCare.Api.DTOs;
using PawCare.Api.Models;
using Xunit;

namespace PawCare.Api.Tests
{
    public class PatientStatusWorkflowTests
    {
        private AppDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new AppDbContext(options);
        }

        private PatientsController CreateControllerWithUser(AppDbContext db, int userId = 1)
        {
            var controller = new PatientsController(db);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, "staff@pawcare.local"),
                new Claim(ClaimTypes.Role, "STAFF")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return controller;
        }

        private async Task<(User user, Ward ward, Owner owner, Patient patient)> SeedBaseDataAsync(AppDbContext db)
        {
            var user = new User
            {
                Id = 1,
                Name = "Dr. Alice",
                Email = "alice@pawcare.local",
                PasswordHash = "hash",
                AccountType = "STAFF",
                IsActive = true
            };

            var ward = new Ward
            {
                Id = 1,
                Name = "ICU Ward",
                Description = "Intensive care",
                Capacity = 5
            };

            var owner = new Owner
            {
                Id = 1,
                Name = "John Doe",
                Email = "john@example.com",
                Phone = "555-0199"
            };

            var patient = new Patient
            {
                Id = 1,
                Name = "Bella",
                Species = "Dog",
                Breed = "Golden Retriever",
                Age = 4,
                Gender = "Female",
                OwnerId = owner.Id,
                Owner = owner,
                WardId = ward.Id,
                Ward = ward,
                AdmissionStatus = AdmissionStatuses.Admitted,
                ClinicalStatus = ClinicalStatuses.UnderTreatment,
                AdmittedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };

            db.Users.Add(user);
            db.Wards.Add(ward);
            db.Owners.Add(owner);
            db.Patients.Add(patient);
            await db.SaveChangesAsync();

            return (user, ward, owner, patient);
        }

        [Fact]
        public async Task ChangeStatus_WithValidClinicalStatus_UpdatesStatusAndAddsAuditHistory()
        {
            var db = CreateDbContext(nameof(ChangeStatus_WithValidClinicalStatus_UpdatesStatusAndAddsAuditHistory));
            await SeedBaseDataAsync(db);
            var controller = CreateControllerWithUser(db, userId: 1);

            var request = new StatusChangeRequest
            {
                NewClinicalStatus = ClinicalStatuses.Stable
            };

            var result = await controller.ChangeStatus(1, request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var patientDto = Assert.IsType<PatientDto>(okResult.Value);
            Assert.Equal(ClinicalStatuses.Stable, patientDto.ClinicalStatus);
            Assert.Equal(AdmissionStatuses.Admitted, patientDto.AdmissionStatus);

            var updatedPatient = await db.Patients.FindAsync(1);
            Assert.NotNull(updatedPatient);
            Assert.Equal(ClinicalStatuses.Stable, updatedPatient.ClinicalStatus);

            var history = await db.StatusHistories.Where(h => h.PatientId == 1).ToListAsync();
            Assert.Single(history);
            Assert.Equal(ClinicalStatuses.UnderTreatment, history[0].OldStatus);
            Assert.Equal(ClinicalStatuses.Stable, history[0].NewStatus);
            Assert.Equal(1, history[0].ChangedByUserId);
        }

        [Fact]
        public async Task ChangeStatus_WithDischarged_SetsDischargedAtAndAuditHistory()
        {
            var db = CreateDbContext(nameof(ChangeStatus_WithDischarged_SetsDischargedAtAndAuditHistory));
            await SeedBaseDataAsync(db);
            var controller = CreateControllerWithUser(db, userId: 1);

            var request = new StatusChangeRequest
            {
                NewClinicalStatus = ClinicalStatuses.Recovering,
                NewAdmissionStatus = AdmissionStatuses.Discharged
            };

            var result = await controller.ChangeStatus(1, request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var patientDto = Assert.IsType<PatientDto>(okResult.Value);
            Assert.Equal(AdmissionStatuses.Discharged, patientDto.AdmissionStatus);
            Assert.Equal(ClinicalStatuses.Recovering, patientDto.ClinicalStatus);
            Assert.NotNull(patientDto.DischargedAt);

            var updatedPatient = await db.Patients.FindAsync(1);
            Assert.NotNull(updatedPatient);
            Assert.NotNull(updatedPatient.DischargedAt);
            Assert.Equal(AdmissionStatuses.Discharged, updatedPatient.AdmissionStatus);

            var histories = await db.StatusHistories.Where(h => h.PatientId == 1).ToListAsync();
            Assert.Equal(2, histories.Count);
            Assert.Contains(histories, h => h.OldStatus == ClinicalStatuses.UnderTreatment && h.NewStatus == ClinicalStatuses.Recovering);
            Assert.Contains(histories, h => h.OldStatus == AdmissionStatuses.Admitted && h.NewStatus == AdmissionStatuses.Discharged);
        }

        [Fact]
        public async Task ChangeStatus_WithDeceased_WithoutConfirmation_ReturnsBadRequest()
        {
            var db = CreateDbContext(nameof(ChangeStatus_WithDeceased_WithoutConfirmation_ReturnsBadRequest));
            await SeedBaseDataAsync(db);
            var controller = CreateControllerWithUser(db, userId: 1);

            var request = new StatusChangeRequest
            {
                NewClinicalStatus = ClinicalStatuses.Critical,
                NewAdmissionStatus = AdmissionStatuses.Deceased,
                Confirm = false
            };

            var result = await controller.ChangeStatus(1, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);

            var patient = await db.Patients.FindAsync(1);
            Assert.NotNull(patient);
            Assert.Equal(AdmissionStatuses.Admitted, patient.AdmissionStatus);
        }

        [Fact]
        public async Task ChangeStatus_WithDeceased_WithConfirmation_UpdatesStatusAndSetsDischargedAt()
        {
            var db = CreateDbContext(nameof(ChangeStatus_WithDeceased_WithConfirmation_UpdatesStatusAndSetsDischargedAt));
            await SeedBaseDataAsync(db);
            var controller = CreateControllerWithUser(db, userId: 1);

            var request = new StatusChangeRequest
            {
                NewClinicalStatus = ClinicalStatuses.Critical,
                NewAdmissionStatus = AdmissionStatuses.Deceased,
                Confirm = true
            };

            var result = await controller.ChangeStatus(1, request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var patientDto = Assert.IsType<PatientDto>(okResult.Value);
            Assert.Equal(AdmissionStatuses.Deceased, patientDto.AdmissionStatus);
            Assert.NotNull(patientDto.DischargedAt);

            var updatedPatient = await db.Patients.FindAsync(1);
            Assert.NotNull(updatedPatient);
            Assert.Equal(AdmissionStatuses.Deceased, updatedPatient.AdmissionStatus);
            Assert.NotNull(updatedPatient.DischargedAt);

            var histories = await db.StatusHistories.Where(h => h.PatientId == 1).ToListAsync();
            Assert.Contains(histories, h => h.OldStatus == AdmissionStatuses.Admitted && h.NewStatus == AdmissionStatuses.Deceased);
        }

        [Fact]
        public async Task ChangeStatus_WithInvalidClinicalStatus_ReturnsBadRequest()
        {
            var db = CreateDbContext(nameof(ChangeStatus_WithInvalidClinicalStatus_ReturnsBadRequest));
            await SeedBaseDataAsync(db);
            var controller = CreateControllerWithUser(db, userId: 1);

            var request = new StatusChangeRequest
            {
                NewClinicalStatus = "INVALID_STATUS"
            };

            var result = await controller.ChangeStatus(1, request);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task ChangeStatus_WithInvalidAdmissionStatus_ReturnsBadRequest()
        {
            var db = CreateDbContext(nameof(ChangeStatus_WithInvalidAdmissionStatus_ReturnsBadRequest));
            await SeedBaseDataAsync(db);
            var controller = CreateControllerWithUser(db, userId: 1);

            var request = new StatusChangeRequest
            {
                NewAdmissionStatus = "UNKNOWN"
            };

            var result = await controller.ChangeStatus(1, request);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task ChangeStatus_WithoutAnyStatus_ReturnsBadRequest()
        {
            var db = CreateDbContext(nameof(ChangeStatus_WithoutAnyStatus_ReturnsBadRequest));
            await SeedBaseDataAsync(db);
            var controller = CreateControllerWithUser(db, userId: 1);

            var request = new StatusChangeRequest();

            var result = await controller.ChangeStatus(1, request);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetStatusHistory_ReturnsChronologicalAuditTrailWithStaffAttribution()
        {
            var db = CreateDbContext(nameof(GetStatusHistory_ReturnsChronologicalAuditTrailWithStaffAttribution));
            await SeedBaseDataAsync(db);

            db.StatusHistories.AddRange(
                new StatusHistory
                {
                    PatientId = 1,
                    OldStatus = "NONE",
                    NewStatus = AdmissionStatuses.Admitted,
                    ChangedByUserId = 1,
                    ChangedAt = DateTime.UtcNow.AddHours(-10)
                },
                new StatusHistory
                {
                    PatientId = 1,
                    OldStatus = ClinicalStatuses.UnderTreatment,
                    NewStatus = ClinicalStatuses.Stable,
                    ChangedByUserId = 1,
                    ChangedAt = DateTime.UtcNow.AddHours(-2)
                }
            );
            await db.SaveChangesAsync();

            var controller = CreateControllerWithUser(db, userId: 1);
            var result = await controller.GetStatusHistory(1);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var historyList = Assert.IsType<List<StatusHistoryDto>>(okResult.Value);

            Assert.Equal(2, historyList.Count);
            Assert.Equal(ClinicalStatuses.Stable, historyList[0].NewStatus);
            Assert.Equal("Dr. Alice", historyList[0].ChangedByUserName);
            Assert.Equal(AdmissionStatuses.Admitted, historyList[1].NewStatus);
        }
    }
}
