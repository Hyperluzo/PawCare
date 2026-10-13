using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.EntityFrameworkCore;
using PawCare.Api.Data;
using PawCare.Api.Models;

namespace PawCare.Api.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationService> _logger;
        private readonly IEmailTemplateService _templateService;

        public NotificationService(
            AppDbContext db,
            IConfiguration configuration,
            ILogger<NotificationService> logger,
            IEmailTemplateService templateService)
        {
            _db = db;
            _configuration = configuration;
            _logger = logger;
            _templateService = templateService;
        }

        public async Task<Notification> SendStatusEmailAsync(Patient patient, string notificationType)
        {
            var recipientEmail = patient.Owner?.Email;
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                // Try reloading patient with owner if not loaded
                var owner = await _db.Owners.FindAsync(patient.OwnerId);
                recipientEmail = owner?.Email ?? "unknown@recipient.local";
            }

            var notification = new Notification
            {
                PatientId = patient.Id,
                OwnerId = patient.OwnerId,
                NotificationType = notificationType,
                RecipientEmail = recipientEmail,
                DeliveryStatus = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            await DispatchEmailAsync(notification, patient);

            return notification;
        }

        public async Task<Notification> RetryNotificationAsync(int notificationId)
        {
            var notification = await _db.Notifications
                .Include(n => n.Patient)
                    .ThenInclude(p => p.Owner)
                .Include(n => n.Patient)
                    .ThenInclude(p => p.Ward)
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification is null)
            {
                throw new KeyNotFoundException($"Notification with ID {notificationId} not found");
            }

            notification.DeliveryStatus = "PENDING";
            notification.ErrorMessage = null;
            await _db.SaveChangesAsync();

            await DispatchEmailAsync(notification, notification.Patient);

            return notification;
        }

        private async Task DispatchEmailAsync(Notification notification, Patient patient)
        {
            var simulationMode = _configuration.GetValue<bool>("Aws:SimulationMode", false);
            var senderEmail = _configuration["Aws:SenderEmail"] ?? "no-reply@pawcare.local";

            try
            {
                var template = _templateService.BuildTemplate(notification.NotificationType, patient);

                if (simulationMode)
                {
                    _logger.LogInformation(
                        "[SIMULATION] SES email to {Recipient} for patient '{PatientName}' (Type: {Type}). Subject: '{Subject}'",
                        notification.RecipientEmail,
                        patient.Name,
                        notification.NotificationType,
                        template.Subject);

                    notification.DeliveryStatus = "SENT";
                    notification.SentAt = DateTime.UtcNow;
                    notification.ErrorMessage = null;
                }
                else
                {
                    using var sesClient = CreateSesClient();

                    var sendRequest = new SendEmailRequest
                    {
                        Source = senderEmail,
                        Destination = new Destination
                        {
                            ToAddresses = new List<string> { notification.RecipientEmail }
                        },
                        Message = new Message
                        {
                            Subject = new Content(template.Subject),
                            Body = new Body
                            {
                                Html = new Content(template.HtmlBody),
                                Text = new Content(template.TextBody)
                            }
                        }
                    };

                    _logger.LogInformation(
                        "Dispatching SES email to {Recipient} from {Sender} for patient {PatientId} (Type: {Type})",
                        notification.RecipientEmail,
                        senderEmail,
                        patient.Id,
                        notification.NotificationType);

                    var response = await sesClient.SendEmailAsync(sendRequest);

                    notification.DeliveryStatus = "SENT";
                    notification.SentAt = DateTime.UtcNow;
                    notification.ErrorMessage = null;

                    _logger.LogInformation(
                        "SES email delivered successfully. MessageId: {MessageId}, HttpStatusCode: {Status}",
                        response.MessageId,
                        response.HttpStatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to deliver email to {Recipient} for notification {NotificationId}: {Error}",
                    notification.RecipientEmail,
                    notification.Id,
                    ex.Message);

                notification.DeliveryStatus = "FAILED";
                notification.ErrorMessage = ex.Message;
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to update notification delivery status in database for {NotificationId}", notification.Id);
            }
        }

        private IAmazonSimpleEmailService CreateSesClient()
        {
            var regionName = _configuration["Aws:Region"] ?? "us-east-1";
            var region = RegionEndpoint.GetBySystemName(regionName);

            var accessKey = _configuration["Aws:AccessKey"];
            var secretKey = _configuration["Aws:SecretKey"];

            if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
            {
                var credentials = new BasicAWSCredentials(accessKey, secretKey);
                return new AmazonSimpleEmailServiceClient(credentials, region);
            }

            // Fallback to AWS SDK standard credential resolution chain (environment variables, AWS profile, EC2 IAM instance profile)
            return new AmazonSimpleEmailServiceClient(region);
        }
    }
}
