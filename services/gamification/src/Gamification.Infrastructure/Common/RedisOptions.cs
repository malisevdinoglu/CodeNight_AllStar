namespace Gamification.Infrastructure.Common;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string Host { get; set; } = "redis";

    public int Port { get; set; } = 6379;
}
