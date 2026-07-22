using Identity.Domain.Enums;

namespace Identity.Domain.Entities;

/// <summary>
/// Iskender.md §1 <c>audit_logs</c>. Case §3.4: her kayıtta kim/ne/ne zaman/nereden/sonuç/detay bulunmalı.
/// </summary>
public class AuditLog
{
    protected AuditLog()
    {
    }

    private AuditLog(
        Guid? userId, AuditActionType actionType, DateTime occurredAt,
        string ipAddress, bool success, string? resourceId, string? details)
    {
        UserId = userId;
        ActionType = actionType;
        OccurredAt = occurredAt;
        IpAddress = ipAddress;
        Success = success;
        ResourceId = resourceId;
        Details = details;
    }

    public long Id { get; private set; }
    public Guid? UserId { get; private set; }
    public AuditActionType ActionType { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public bool Success { get; private set; }
    public string? ResourceId { get; private set; }

    /// <summary>Serbest biçim JSON (jsonb) — ek bağlam (örn. ihlal edilen kural).</summary>
    public string? Details { get; private set; }

    public static AuditLog Create(
        Guid? userId, AuditActionType actionType, DateTime occurredAtUtc,
        string ipAddress, bool success, string? resourceId = null, string? details = null) =>
        new(userId, actionType, occurredAtUtc, ipAddress, success, resourceId, details);
}
