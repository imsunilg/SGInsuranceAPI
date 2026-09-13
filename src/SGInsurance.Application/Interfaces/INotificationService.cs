namespace SGInsurance.Application.Interfaces;

/// <summary>
/// Dummy notification service: looks up a notification_templates row, substitutes
/// {{placeholder}} tokens from dataObject, inserts a notifications row, writes an
/// audit_logs row, and (optionally) writes the rendered HTML under
/// generated-documents/emails/{guid}.html. No real email/SMS is sent - this is a
/// local learning app.
/// </summary>
public interface INotificationService
{
    Task<string> SendAsync(string templateCode, Guid? customerId, string recipient, IDictionary<string, string?> data);
}
