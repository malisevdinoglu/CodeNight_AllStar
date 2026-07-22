namespace Campaign.Application.Common;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
