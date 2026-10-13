using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PawCare.Api.Controllers;
using PawCare.Api.Data;
using PawCare.Api.DTOs;
using PawCare.Api.Models;
using PawCare.Api.Services;
using Xunit;

namespace PawCare.Api.Tests
{
    public class NotificationServiceTests
    {
        private AppDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new AppDbContext(options);
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
                Email = "john.doe@example.com",
                Phone = "555-0199",
                CreatedAt = DateTime.UtcNow
            };

            var patient = new Patient
            {
                Id = 1,
                Name = "Barnaby",
                Species = "Canine",
                Breed = "Golden Retriever",
                Age = 4,
                Gender = "Male",
                OwnerId = 1,
                Owner = owner,
                WardId = 1,
                Ward = ward,
                AdmissionStatus = AdmissionStatuses.Admitted,
                ClinicalStatus = ClinicalStatuses.UnderTreatment,
                AdmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            db.Wards.Add(ward);
            db.Owners.Add(owner);
            db.Patients.Add(patient);
            await db.SaveChangesAsync();

            return (user, ward, owner, patient);
        }

        [Fact]
        public void EmailTemplateService_BuildsUnderTreatmentTemplate_WithInterpolatedData()
        {
            var templateService = new EmailTemplateService();
            var patient = new Patient
            {
                Name = "Barnaby",
                Species = "Canine",
                Breed = "Golden Retriever",
                ClinicalStatus = "UNDER_TREATMENT",
                AdmissionStatus = "ADMITTED",
                Owner = new Owner { Name = "John Doe", Email = "john@example.com", Phone = "123" },
                Ward = new Ward { Name = "General Ward", Description = "Gen", Capacity = 10 }
            };

            var template = templateService.BuildTemplate("UNDER_TREATMENT", patient);

            Assert.Contains("Barnaby", template.Subject);
            Assert.Contains("UNDER TREATMENT", template.Subject);
            Assert.Contains("John Doe", template.HtmlBody);
            Assert.Contains("Golden Retriever", template.HtmlBody);
            Assert.Contains("General Ward", template.HtmlBody);
            Assert.Contains("PawCare Veterinary Hospital", template.HtmlBody);
        }

        [Fact]
        public void EmailTemplateService_BuildsDischargedTemplate_WithInstructions()
        {
            var templateService = new EmailTemplateService();
            var patient = new Patient
            {
                Name = "Barnaby",
                Species = "Canine",
                Breed = "Golden Retriever",
                ClinicalStatus = "STABLE",
                AdmissionStatus = "DISCHARGED",
                Owner = new Owner { Name = "John Doe", Email = "john@example.com", Phone = "123" },
                Ward = new Ward { Name = "General Ward", Description = "Gen", Capacity = 10 }
            };

            var template = templateService.BuildTemplate("DISCHARGED", patient);

            Assert.Contains("Barnaby is Discharged", template.Subject);
            Assert.Contains("John Doe", template.HtmlBody);
            Assert.Contains("DISCHARGE", template.HtmlBody);
            Assert.Contains("Pick-Up Checklist", template.HtmlBody);
        }

        [Fact]
        public void EmailTemplateService_BuildsDeceasedTemplate_WithSympathyAndMemorialSupport()
        {
            var templateService = new EmailTemplateService();
            var patient = new Patient
            {
                Name = "Barnaby",
                Species = "Canine",
                Breed = "Golden Retriever",
                ClinicalStatus = "CRITICAL",
                AdmissionStatus = "DECEASED",
                Owner = new Owner { Name = "John Doe", Email = "john@example.com", Phone = "123" },
                Ward = new Ward { Name = "ICU", Description = "ICU", Capacity = 5 }
            };

            var template = templateService.BuildTemplate("DECEASED", patient);

            Assert.Contains("Heartfelt Condolences Regarding Barnaby", template.Subject);
            Assert.Contains("John Doe", template.HtmlBody);
            Assert.Contains("deepest sympathies", template.HtmlBody);
            Assert.Contains("aftercare", template.HtmlBody);
        }

        [Fact]
        public async Task NotificationService_SimulationMode_MarksNotificationAsSent()
        {
            var db = CreateDbContext("Notification_Sim_Test");
            var (_, _, owner, patient) = await SeedBaseDataAsync(db);

            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Aws:SimulationMode", "true" },
                { "Aws:SenderEmail", "test-sender@pawcare.local" }
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
            var templateService = new EmailTemplateService();

            var service = new NotificationService(
                db,
                configuration,
                NullLogger<NotificationService>.Instance,
                templateService);

            var notification = await service.SendStatusEmailAsync(patient, "UNDER_TREATMENT");

            Assert.NotNull(notification);
            Assert.Equal("SENT", notification.DeliveryStatus);
            Assert.NotNull(notification.SentAt);
            Assert.Null(notification.ErrorMessage);
            Assert.Equal(owner.Email, notification.RecipientEmail);

            var stored = await db.Notifications.FirstOrDefaultAsync(n => n.Id == notification.Id);
            Assert.NotNull(stored);
            Assert.Equal("SENT", stored.DeliveryStatus);
        }

        [Fact]
        public async Task NotificationService_WhenSesFails_LogsDeliveryAsFailedWithoutThrowing()
        {
            var db = CreateDbContext("Notification_Fail_Test");
            var (_, _, owner, patient) = await SeedBaseDataAsync(db);

            // SimulationMode false, invalid credentials/endpoint will fail
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Aws:SimulationMode", "false" },
                { "Aws:SenderEmail", "test-sender@pawcare.local" },
                { "Aws:AccessKey", "INVALID_KEY" },
                { "Aws:SecretKey", "INVALID_SECRET" },
                { "Aws:Region", "us-east-1" }
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
            var templateService = new EmailTemplateService();

            var service = new NotificationService(
                db,
                configuration,
                NullLogger<NotificationService>.Instance,
                templateService);

            // Must NOT throw exception to caller
            var notification = await service.SendStatusEmailAsync(patient, "UNDER_TREATMENT");

            Assert.NotNull(notification);
            Assert.Equal("FAILED", notification.DeliveryStatus);
            Assert.NotNull(notification.ErrorMessage);
            Assert.Null(notification.SentAt);

            var stored = await db.Notifications.FirstOrDefaultAsync(n => n.Id == notification.Id);
            Assert.NotNull(stored);
            Assert.Equal("FAILED", stored.DeliveryStatus);
            Assert.NotNull(stored.ErrorMessage);
        }

        [Fact]
        public async Task PatientsController_ChangeStatus_DecoupledFromNotificationFailure()
        {
            var db = CreateDbContext("Patients_Status_Decoupled_Test");
            var (user, _, _, patient) = await SeedBaseDataAsync(db);

            // Simulate broken SES
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Aws:SimulationMode", "false" },
                { "Aws:AccessKey", "BAD_KEY" },
                { "Aws:SecretKey", "BAD_SECRET" }
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
            var notificationService = new NotificationService(
                db,
                configuration,
                NullLogger<NotificationService>.Instance,
                new EmailTemplateService());

            var controller = new PatientsController(
                db,
                notificationService,
                NullLogger<PatientsController>.Instance);

            var claimsUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, "STAFF")
            }, "TestAuth"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsUser }
            };

            var request = new StatusChangeRequest
            {
                NewClinicalStatus = ClinicalStatuses.Stable
            };

            var result = await controller.ChangeStatus(patient.Id, request);

            // Status change still succeeds with 200 OK
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<PatientDto>(okResult.Value);
            Assert.Equal(ClinicalStatuses.Stable, dto.ClinicalStatus);

            // And DB reflects the new status
            var updatedPatient = await db.Patients.FindAsync(patient.Id);
            Assert.Equal(ClinicalStatuses.Stable, updatedPatient!.ClinicalStatus);

            // And the notification is logged as FAILED, not lost
            var notification = await db.Notifications.FirstOrDefaultAsync(n => n.PatientId == patient.Id);
            Assert.NotNull(notification);
            Assert.Equal("FAILED", notification.DeliveryStatus);
            Assert.NotEmpty(notification.ErrorMessage!);
        }

        [Fact]
        public async Task AdminNotificationsController_GetAllAndRetry_WorksCorrectly()
        {
            var db = CreateDbContext("Admin_Notifications_Test");
            var (_, _, owner, patient) = await SeedBaseDataAsync(db);

            var failedNotification = new Notification
            {
                PatientId = patient.Id,
                OwnerId = owner.Id,
                NotificationType = "UNDER_TREATMENT",
                RecipientEmail = owner.Email,
                DeliveryStatus = "FAILED",
                ErrorMessage = "SES connection timed out",
                CreatedAt = DateTime.UtcNow
            };
            db.Notifications.Add(failedNotification);
            await db.SaveChangesAsync();

            var templateService = new EmailTemplateService();
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Aws:SimulationMode", "true" }
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
            var notificationService = new NotificationService(
                db,
                configuration,
                NullLogger<NotificationService>.Instance,
                templateService);

            var controller = new AdminNotificationsController(db, notificationService, templateService);

            // 1. GetAll with status filter
            var listResult = await controller.GetAll(status: "FAILED");
            var okList = Assert.IsType<OkObjectResult>(listResult.Result);
            var listDto = Assert.IsType<NotificationListResponseDto>(okList.Value);
            Assert.Single(listDto.Notifications);
            Assert.Equal("FAILED", listDto.Notifications[0].DeliveryStatus);

            // 2. GetById includes rendered email preview
            var detailResult = await controller.GetById(failedNotification.Id);
            var okDetail = Assert.IsType<OkObjectResult>(detailResult.Result);
            var detailDto = Assert.IsType<NotificationDetailDto>(okDetail.Value);
            Assert.Contains("Barnaby", detailDto.Subject);
            Assert.Contains("<!DOCTYPE html>", detailDto.BodyHtml);

            // 3. Retry notification
            var retryResult = await controller.Retry(failedNotification.Id);
            var okRetry = Assert.IsType<OkObjectResult>(retryResult.Result);
            var retryDto = Assert.IsType<RetryNotificationResponseDto>(okRetry.Value);
            Assert.True(retryDto.Success);
            Assert.Equal("SENT", retryDto.Notification!.DeliveryStatus);

            // Confirm database was updated
            var reloaded = await db.Notifications.FindAsync(failedNotification.Id);
            Assert.Equal("SENT", reloaded!.DeliveryStatus);
            Assert.Null(reloaded.ErrorMessage);
            Assert.NotNull(reloaded.SentAt);
        }
    }
}
