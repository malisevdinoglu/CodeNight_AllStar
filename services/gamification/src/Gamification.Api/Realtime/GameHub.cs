using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gamification.Api.Realtime;

/// <summary>
/// Mali.md §7: sadece SUNUCU -&gt; İSTEMCİ push (badge.earned/points.updated). İstemcinin
/// çağırabileceği bir hub metodu yok — bağlantı kurulup JWT ile kimliği doğrulanması yeterli;
/// SubClaimUserIdProvider "sub" claim'ini Context.UserIdentifier'a bağlar, GameNotifier de
/// Clients.User(expertId) ile hedefli gönderim yapar.
/// </summary>
[Authorize]
public sealed class GameHub : Hub
{
}
