using Dashboard.Models;
using Dashboard.Models.ViewModels;

namespace Dashboard.Services.Interfaces;

public interface INotificationConfigCache
{
    Task<List<NotificationConfig>> GetAllConfigsAsync();
    Task<NotificationConfig?> GetConfigAsync(string notificationCode);
    Task InvalidateAsync();
}
