using Dashboard.Models;
using Dashboard.Models.ViewModels;

namespace Dashboard.Services.Interfaces;

public interface INotificationService
{
    Task SendToUserAsync(string userId, NotificationSignalDto notification);
    Task SendToRoleAsync(string role, NotificationSignalDto notification);
    Task<List<NotificationListItemDto>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20);
    Task<int> GetUnreadCountAsync(string userId);
    Task<NotificationListItemDto?> GetByIdAsync(int notificationId);
    Task MarkAsReadAsync(int notificationId, string userId);
    Task MarkAllAsReadAsync(string userId);
    Task<Notification> SaveNotificationAsync(NotificationSignalDto dto, string userId);
}
