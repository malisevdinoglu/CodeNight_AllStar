namespace Gamification.Application.Common;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
