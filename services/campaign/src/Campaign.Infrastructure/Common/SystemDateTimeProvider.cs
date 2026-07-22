using Campaign.Application.Common;

namespace Campaign.Infrastructure.Common;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
