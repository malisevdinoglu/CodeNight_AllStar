namespace Gamification.Application.External;

public sealed record IdentityUserDto(Guid Id, string FirstName, string LastName);

/// <summary>
/// Campaign.Application.External.IIdentityServiceClient ile aynı desen (Core_Principles §6:
/// servisler-arası çağrı X-Internal-Api-Key ile, Gateway atlanır). Gamification, event
/// payload'larında taşınmayan görünen adı (display name) SADECE okuma anında ("cold path",
/// leaderboard/profile sorgularında) bu istemciyle çözer — event işleme (Process* handler'ları)
/// asla senkron bu servise bağımlı olmaz (Core_Principles §3 graceful degradation: Identity
/// erişilemezse isim "Bilinmeyen" olarak düşer, puanlama/rozet mantığı ETKİLENMEZ).
/// </summary>
public interface IIdentityServiceClient
{
    Task<IReadOnlyList<IdentityUserDto>> GetExpertsAsync(CancellationToken cancellationToken = default);
}
