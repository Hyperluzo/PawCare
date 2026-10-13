using PawCare.Api.Models;

namespace PawCare.Api.Services
{
    public interface INotificationService
    {
        Task<Notification> SendStatusEmailAsync(Patient patient, string notificationType);
        Task<Notification> RetryNotificationAsync(int notificationId);
    }
}
