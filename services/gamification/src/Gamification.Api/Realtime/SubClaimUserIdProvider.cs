using Microsoft.AspNetCore.SignalR;

namespace Gamification.Api.Realtime;

/// <summary>
/// SignalR'ın varsayılan IUserIdProvider'ı ClaimTypes.NameIdentifier bekler, ama bu projede
/// JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear() ile claim isimleri HAM tutulur
/// (Identity/Campaign ile aynı gerekçe: "sub"/"role"). Bu yüzden kullanıcı kimliği burada
/// açıkça "sub" claim'inden okunur - aksi halde Clients.User(expertId) HİÇBİR ZAMAN eşleşmez.
/// </summary>
public sealed class SubClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst("sub")?.Value;
}
