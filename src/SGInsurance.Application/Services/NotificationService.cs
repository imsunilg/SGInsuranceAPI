using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SGInsurance.Application.Interfaces;
using SGInsurance.Domain.Entities;

namespace SGInsurance.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationTemplateRepository _templates;
    private readonly INotificationRepository _notifications;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationService> _logger;
    private static readonly Regex PlaceholderRegex = new("{{\\s*(\\w+)\\s*}}", RegexOptions.Compiled);

    public NotificationService(
        INotificationTemplateRepository templates,
        INotificationRepository notifications,
        IAuditLogRepository auditLogs,
        IConfiguration config,
        ILogger<NotificationService> logger)
    {
        _templates = templates;
        _notifications = notifications;
        _auditLogs = auditLogs;
        _config = config;
        _logger = logger;
    }

    public async Task<string> SendAsync(string templateCode, Guid? customerId, string recipient, IDictionary<string, string?> data)
    {
        var template = await _templates.GetByCodeAsync(templateCode)
            ?? throw new InvalidOperationException($"Notification template '{templateCode}' not found.");

        var rendered = PlaceholderRegex.Replace(template.BodyTemplate, m =>
        {
            var key = m.Groups[1].Value;
            return data.TryGetValue(key, out var value) ? value ?? string.Empty : m.Value;
        });

        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            CustomerId = customerId,
            TemplateCode = templateCode,
            Channel = template.Channel,
            Recipient = recipient,
            Payload = JsonSerializer.Serialize(data),
            Status = "SENT",
            SentAt = DateTimeOffset.UtcNow
        };
        await _notifications.AddAsync(notification);

        await _auditLogs.AddAsync(new AuditLog
        {
            AuditId = Guid.NewGuid(),
            UserId = null,
            Action = "NOTIFICATION_SENT",
            EntityType = "Notification",
            EntityId = notification.NotificationId.ToString(),
            Details = JsonSerializer.Serialize(new { templateCode, recipient }),
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _notifications.SaveChangesAsync();

        TryWriteHtmlFile(notification.NotificationId, template.Subject, rendered);

        _logger.LogInformation("Notification {TemplateCode} sent to {Recipient}", templateCode, recipient);

        return rendered;
    }

    private void TryWriteHtmlFile(Guid notificationId, string? subject, string bodyHtml)
    {
        try
        {
            var root = _config["GeneratedDocuments:RootPath"] ?? "generated-documents";
            var dir = Path.Combine(root, "emails");
            Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, $"{notificationId}.html");
            var html = $"<html><head><title>{subject}</title></head><body>{bodyHtml}</body></html>";
            File.WriteAllText(filePath, html);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write notification email file for {NotificationId}", notificationId);
        }
    }
}
