using Identity.Domain.Entities;

namespace Identity.Application.Common;

public interface IAuditLogRepository
{
    void Add(AuditLog auditLog);

    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);
}
