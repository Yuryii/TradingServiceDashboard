using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Dashboard.Data;
using Dashboard.Hubs;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;

namespace Dashboard.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan DedupTtl = TimeSpan.FromHours(1);

    public NotificationService(
        ApplicationDbContext context,
        IHubContext<NotificationHub> hubContext,
        IMemoryCache cache)
    {
        _context = context;
        _hubContext = hubContext;
        _cache = cache;
    }

    public async Task<Notification> SaveNotificationAsync(NotificationSignalDto dto, string userId)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = dto.Title,
            Message = dto.Message,
            Category = dto.Category,
            Severity = dto.Severity,
            IconClass = dto.IconClass,
            IconBgClass = dto.IconBgClass,
            ActionUrl = dto.ActionUrl,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        dto.NotificationID = notification.NotificationID;
        dto.CreatedAt = notification.CreatedAt;

        // Send real-time via SignalR
        await _hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", dto);

        return notification;
    }

    public async Task SendToUserAsync(string userId, NotificationSignalDto dto)
    {
        var dedupKey = $"notif:{dto.Category}:{dto.Severity}:{dto.Title.GetHashCode()}";
        if (_cache.TryGetValue(dedupKey, out _))
            return;

        _cache.Set(dedupKey, true, DedupTtl);

        dto.CreatedAt = DateTime.UtcNow;
        dto.TimeAgo = GetTimeAgo(dto.CreatedAt);

        await SaveNotificationAsync(dto, userId);
    }

    public async Task SendToRoleAsync(string role, NotificationSignalDto dto)
    {
        var dedupKey = $"notif_role:{dto.Category}:{dto.Severity}:{dto.Title.GetHashCode()}:{role}";
        if (_cache.TryGetValue(dedupKey, out _))
            return;

        _cache.Set(dedupKey, true, DedupTtl);

        var users = await _context.Users
            .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && _context.Roles.Any(r => r.Name == role)))
            .ToListAsync();

        dto.CreatedAt = DateTime.UtcNow;
        dto.TimeAgo = GetTimeAgo(dto.CreatedAt);

        foreach (var user in users)
        {
            await SaveNotificationAsync(dto, user.Id);
        }
    }

    public async Task<List<NotificationListItemDto>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationListItemDto
            {
                NotificationID = n.NotificationID,
                Title = n.Title,
                Message = n.Message,
                Category = n.Category,
                Severity = n.Severity,
                IconClass = n.IconClass,
                IconBgClass = n.IconBgClass,
                ActionUrl = n.ActionUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                TimeAgo = n.CreatedAt.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss")
            })
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<NotificationListItemDto?> GetByIdAsync(int notificationId)
    {
        var n = await _context.Notifications.FindAsync(notificationId);
        if (n == null) return null;

        return new NotificationListItemDto
        {
            NotificationID = n.NotificationID,
            Title = n.Title,
            Message = n.Message,
            Category = n.Category,
            Severity = n.Severity,
            IconClass = n.IconClass,
            IconBgClass = n.IconBgClass,
            ActionUrl = n.ActionUrl,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            TimeAgo = GetTimeAgo(n.CreatedAt)
        };
    }

    public async Task MarkAsReadAsync(int notificationId, string userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationID == notificationId && n.UserId == userId);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private static string GetTimeAgo(DateTime utcTime)
    {
        var local = utcTime.ToLocalTime();
        var span = DateTime.Now - local;

        if (span.TotalMinutes < 1) return "Vừa xong";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} ngày trước";
        return local.ToString("dd/MM/yyyy HH:mm");
    }
}
