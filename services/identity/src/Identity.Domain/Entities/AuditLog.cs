namespace Identity.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }

    /// <summary>Basarisiz giris denemesinde kullanici bilinmeyebilir.</summary>
    public Guid? UserId { get; set; }

    public string ActionType { get; set; } = null!;
    public DateTimeOffset OccurredAt { get; set; }
    public string IpAddress { get; set; } = null!;
    public bool Success { get; set; }
    public string? ResourceId { get; set; }

    /// <summary>jsonb — islem detayi (orn. degisen rol, hedef kayit).</summary>
    public string? Details { get; set; }
}
