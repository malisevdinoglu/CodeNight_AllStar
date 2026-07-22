namespace Campaign.Application.Common;

/// <summary>HTTP isteğinden türeyen bağlam — Api katmanı implemente eder (Identity ile aynı sözleşme).</summary>
public interface ICurrentRequestContext
{
    Guid? UserId { get; }

    string? Role { get; }

    IReadOnlyList<string> Expertise { get; }

    string IpAddress { get; }
}
