using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawCare.Api.Data;
using PawCare.Api.DTOs;
using PawCare.Api.Services;

namespace PawCare.Api.Controllers
{
    [ApiController]
    [Route("api/admin/notifications")]
    [Authorize(Roles = "ADMIN")]
    public class AdminNotificationsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly INotificationService _notificationService;
        private readonly IEmailTemplateService _templateService;

        public AdminNotificationsController(
            AppDbContext db,
            INotificationService notificationService,
            IEmailTemplateService templateService)
        {
            _db = db;
            _notificationService = notificationService;
            _templateService = templateService;
        }

        [HttpGet]
        public async Task<ActionResult<NotificationListResponseDto>> GetAll(
            [FromQuery] string? status = null,
            [FromQuery] int? patientId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _db.Notifications
                .Include(n => n.Patient)
                .Include(n => n.Owner)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normStatus = status.Trim().ToUpperInvariant();
                query = query.Where(n => n.DeliveryStatus == normStatus);
            }

            if (patientId.HasValue)
            {
                query = query.Where(n => n.PatientId == patientId.Value);
            }

            var total = await query.CountAsync();

            var list = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    PatientId = n.PatientId,
                    PatientName = n.Patient != null ? n.Patient.Name : "Unknown",
                    OwnerId = n.OwnerId,
                    OwnerName = n.Owner != null ? n.Owner.Name : "Unknown",
                    NotificationType = n.NotificationType,
                    RecipientEmail = n.RecipientEmail,
                    DeliveryStatus = n.DeliveryStatus,
                    ErrorMessage = n.ErrorMessage,
                    SentAt = n.SentAt,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(new NotificationListResponseDto
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Notifications = list
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationDetailDto>> GetById(int id)
        {
            var notification = await _db.Notifications
                .Include(n => n.Patient)
                    .ThenInclude(p => p.Owner)
                .Include(n => n.Patient)
                    .ThenInclude(p => p.Ward)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification is null)
            {
                return NotFound(new { message = $"Notification with ID {id} not found" });
            }

            var template = _templateService.BuildTemplate(notification.NotificationType, notification.Patient);

            var detail = new NotificationDetailDto
            {
                Id = notification.Id,
                PatientId = notification.PatientId,
                PatientName = notification.Patient.Name,
                OwnerId = notification.OwnerId,
                OwnerName = notification.Patient.Owner?.Name ?? "Unknown",
                NotificationType = notification.NotificationType,
                RecipientEmail = notification.RecipientEmail,
                DeliveryStatus = notification.DeliveryStatus,
                ErrorMessage = notification.ErrorMessage,
                SentAt = notification.SentAt,
                CreatedAt = notification.CreatedAt,
                Subject = template.Subject,
                BodyHtml = template.HtmlBody,
                BodyText = template.TextBody
            };

            return Ok(detail);
        }

        [HttpPost("{id}/retry")]
        public async Task<ActionResult<RetryNotificationResponseDto>> Retry(int id)
        {
            var exists = await _db.Notifications.AnyAsync(n => n.Id == id);
            if (!exists)
            {
                return NotFound(new { message = $"Notification with ID {id} not found" });
            }

            try
            {
                var updated = await _notificationService.RetryNotificationAsync(id);

                var dto = new NotificationDto
                {
                    Id = updated.Id,
                    PatientId = updated.PatientId,
                    PatientName = updated.Patient?.Name ?? "Unknown",
                    OwnerId = updated.OwnerId,
                    OwnerName = updated.Patient?.Owner?.Name ?? "Unknown",
                    NotificationType = updated.NotificationType,
                    RecipientEmail = updated.RecipientEmail,
                    DeliveryStatus = updated.DeliveryStatus,
                    ErrorMessage = updated.ErrorMessage,
                    SentAt = updated.SentAt,
                    CreatedAt = updated.CreatedAt
                };

                return Ok(new RetryNotificationResponseDto
                {
                    Success = updated.DeliveryStatus == "SENT",
                    Message = updated.DeliveryStatus == "SENT"
                        ? "Notification email dispatched successfully via SES."
                        : $"Retry failed: {updated.ErrorMessage}",
                    Notification = dto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RetryNotificationResponseDto
                {
                    Success = false,
                    Message = $"Retry attempt encountered an error: {ex.Message}",
                    Notification = null
                });
            }
        }
    }
}
