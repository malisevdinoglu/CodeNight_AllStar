namespace Identity.Application.Common;

/// <summary>Testedilebilirlik için (kilitleme/süre hesapları sabit "now" ile test edilir).</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
