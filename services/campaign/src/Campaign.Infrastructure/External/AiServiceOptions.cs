namespace Campaign.Infrastructure.External;

public sealed class AiServiceOptions
{
    public const string SectionName = "AiService";

    public string BaseUrl { get; set; } = string.Empty;
}
