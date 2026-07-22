namespace Gamification.Infrastructure.Common;

public sealed class IdentityServiceOptions
{
    public const string SectionName = "IdentityService";

    public string BaseUrl { get; set; } = "http://identity-api:8080";
}
