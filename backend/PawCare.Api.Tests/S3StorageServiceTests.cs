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
using System.Text;

namespace PawCare.Api.Tests
{
    public class S3StorageServiceTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private IConfiguration CreateConfiguration(Dictionary<string, string?>? customSettings = null)
        {
            var dict = new Dictionary<string, string?>
            {
                { "Aws:Region", "us-east-1" },
                { "Aws:S3:BucketName", "pawcare-attachments-test" },
                { "Aws:S3:SimulationMode", "true" },
                { "Aws:S3:MaxFileSizeBytes", "10485760" } // 10MB
            };

            if (customSettings != null)
            {
                foreach (var kv in customSettings)
                {
                    dict[kv.Key] = kv.Value;
                }
            }

            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        [Fact]
        public async Task UploadFileAsync_WithValidFile_ReturnsS3UrlInSimulationMode()
        {
            // Arrange
            var config = CreateConfiguration();
            var service = new S3StorageService(config, NullLogger<S3StorageService>.Instance);

            var content = "Sample medical report data";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            var url = await service.UploadFileAsync(stream, "blood_test.pdf", "application/pdf", "reports");

            // Assert
            Assert.NotNull(url);
            Assert.StartsWith("https://pawcare-attachments-test.s3.us-east-1.amazonaws.com/reports/", url);
            Assert.EndsWith(".pdf", url);
            Assert.True(service.IsSimulationMode);
            Assert.Equal("pawcare-attachments-test", service.BucketName);
        }

        [Fact]
        public async Task UploadFileAsync_WithEmptyStream_ThrowsArgumentException()
        {
            // Arrange
            var config = CreateConfiguration();
            var service = new S3StorageService(config, NullLogger<S3StorageService>.Instance);
            using var stream = new MemoryStream();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UploadFileAsync(stream, "empty.png", "image/png"));
        }

        [Fact]
        public async Task UploadFileAsync_WithDisallowedExtension_ThrowsInvalidOperationException()
        {
            // Arrange
            var config = CreateConfiguration();
            var service = new S3StorageService(config, NullLogger<S3StorageService>.Instance);

            var content = "Malicious executable";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UploadFileAsync(stream, "script.exe", "application/octet-stream"));

            Assert.Contains("is not permitted", ex.Message);
        }

        [Fact]
        public async Task UploadFileAsync_ExceedingSizeLimit_ThrowsInvalidOperationException()
        {
            // Arrange
            var config = CreateConfiguration(new Dictionary<string, string?>
            {
                { "Aws:S3:MaxFileSizeBytes", "100" } // 100 bytes limit
            });
            var service = new S3StorageService(config, NullLogger<S3StorageService>.Instance);

            var bigData = new byte[500];
            using var stream = new MemoryStream(bigData);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UploadFileAsync(stream, "photo.jpg", "image/jpeg"));

            Assert.Contains("exceeds maximum allowed size", ex.Message);
        }

        [Fact]
        public async Task DeleteFileAsync_WithValidUrl_ReturnsTrue()
        {
            // Arrange
            var config = CreateConfiguration();
            var service = new S3StorageService(config, NullLogger<S3StorageService>.Instance);
            var fileUrl = "https://pawcare-attachments-test.s3.us-east-1.amazonaws.com/patients/photos/sample.jpg";

            // Act
            var result = await service.DeleteFileAsync(fileUrl);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task Controller_UploadPhoto_UpdatesPatientPhotoUrl()
        {
            // Arrange
            using var db = CreateDbContext();
            var owner = new Owner { Name = "Jane Doe", Email = "jane@example.com", Phone = "555-1234" };
            var ward = new Ward { Name = "ICU", Description = "Critical Care", Capacity = 5 };
            db.Owners.Add(owner);
            db.Wards.Add(ward);
            await db.SaveChangesAsync();

            var patient = new Patient
            {
                Name = "Max",
                Species = "Canine",
                Breed = "Golden Retriever",
                Age = 4,
                Gender = "Male",
                OwnerId = owner.Id,
                WardId = ward.Id,
                AdmissionStatus = "ADMITTED",
                ClinicalStatus = "UNDER_TREATMENT"
            };
            db.Patients.Add(patient);
            await db.SaveChangesAsync();

            var config = CreateConfiguration();
            var storage = new S3StorageService(config, NullLogger<S3StorageService>.Instance);
            var controller = new PatientsController(db, null, storage, config);

            var fileContent = "Fake image content for testing";
            var fileBytes = Encoding.UTF8.GetBytes(fileContent);
            var file = new FormFile(new MemoryStream(fileBytes), 0, fileBytes.Length, "file", "max_photo.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            // Act
            var actionResult = await controller.UploadPhoto(patient.Id, file);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var responseDto = Assert.IsType<FileUploadResponseDto>(okResult.Value);
            Assert.NotNull(responseDto.FileUrl);
            Assert.Contains("patients/photos", responseDto.FileUrl);

            var updatedPatient = await db.Patients.FindAsync(patient.Id);
            Assert.NotNull(updatedPatient?.PhotoUrl);
            Assert.Equal(responseDto.FileUrl, updatedPatient.PhotoUrl);
        }

        [Fact]
        public async Task Controller_UploadTreatmentAttachment_UpdatesTreatmentAttachmentFields()
        {
            // Arrange
            using var db = CreateDbContext();
            var owner = new Owner { Name = "Alex Smith", Email = "alex@example.com", Phone = "555-4321" };
            var ward = new Ward { Name = "General", Description = "General Ward", Capacity = 10 };
            var staff = new User { Name = "Dr. Dave", Email = "dave@pawcare.local", PasswordHash = "hash", AccountType = "STAFF" };
            db.Owners.Add(owner);
            db.Wards.Add(ward);
            db.Users.Add(staff);
            await db.SaveChangesAsync();

            var patient = new Patient
            {
                Name = "Bella",
                Species = "Feline",
                Breed = "Persian",
                Age = 2,
                Gender = "Female",
                OwnerId = owner.Id,
                WardId = ward.Id
            };
            db.Patients.Add(patient);
            await db.SaveChangesAsync();

            var treatment = new Treatment
            {
                PatientId = patient.Id,
                Diagnosis = "Respiratory Infection",
                TreatmentPlan = "Antibiotics 50mg daily",
                AddedByUserId = staff.Id
            };
            db.Treatments.Add(treatment);
            await db.SaveChangesAsync();

            var config = CreateConfiguration();
            var storage = new S3StorageService(config, NullLogger<S3StorageService>.Instance);
            var controller = new PatientsController(db, null, storage, config);

            var reportContent = "Chest X-Ray scan observations";
            var fileBytes = Encoding.UTF8.GetBytes(reportContent);
            var file = new FormFile(new MemoryStream(fileBytes), 0, fileBytes.Length, "file", "chest_xray.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            // Act
            var actionResult = await controller.UploadTreatmentAttachment(patient.Id, treatment.Id, file);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var responseDto = Assert.IsType<FileUploadResponseDto>(okResult.Value);
            Assert.NotNull(responseDto.FileUrl);
            Assert.Contains("treatments/attachments", responseDto.FileUrl);
            Assert.Equal("chest_xray.pdf", responseDto.FileName);

            var updatedTreatment = await db.Treatments.FindAsync(treatment.Id);
            Assert.NotNull(updatedTreatment?.AttachmentUrl);
            Assert.Equal("chest_xray.pdf", updatedTreatment.AttachmentFileName);
            Assert.Equal(responseDto.FileUrl, updatedTreatment.AttachmentUrl);
        }

        [Fact]
        public async Task Controller_UploadPhoto_RejectsInvalidExtension()
        {
            // Arrange
            using var db = CreateDbContext();
            var patient = new Patient { Name = "Rocky", Species = "Canine", Breed = "Boxer", Gender = "Male" };
            db.Patients.Add(patient);
            await db.SaveChangesAsync();

            var config = CreateConfiguration();
            var storage = new S3StorageService(config, NullLogger<S3StorageService>.Instance);
            var controller = new PatientsController(db, null, storage, config);

            var fileBytes = Encoding.UTF8.GetBytes("Bad file content");
            var file = new FormFile(new MemoryStream(fileBytes), 0, fileBytes.Length, "file", "virus.exe")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/x-msdownload"
            };

            // Act
            var actionResult = await controller.UploadPhoto(patient.Id, file);

            // Assert
            Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        }
    }
}
