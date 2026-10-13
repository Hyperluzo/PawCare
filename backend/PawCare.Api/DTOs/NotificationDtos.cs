namespace PawCare.Api.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = default!;
        public int OwnerId { get; set; }
        public string OwnerName { get; set; } = default!;
        public string NotificationType { get; set; } = default!;
        public string RecipientEmail { get; set; } = default!;
        public string DeliveryStatus { get; set; } = default!; // PENDING, SENT, FAILED
        public string? ErrorMessage { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationDetailDto : NotificationDto
    {
        public string Subject { get; set; } = default!;
        public string BodyHtml { get; set; } = default!;
        public string BodyText { get; set; } = default!;
    }

    public class NotificationListResponseDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<NotificationDto> Notifications { get; set; } = new();
    }

    public class RetryNotificationResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = default!;
        public NotificationDto? Notification { get; set; }
    }
}
