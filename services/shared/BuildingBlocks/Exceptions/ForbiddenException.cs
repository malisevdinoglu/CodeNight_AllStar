namespace BuildingBlocks.Exceptions;

/// <summary>
/// Kimlik doğrulandı ama rol/yetki yetersiz (403) — Core_Principles §6/§10: "rol uymuyorsa
/// 403 + audit log". Handler'larda IDOR/yetki kontrolleri için kullanılır
/// (örn. offer.subscriberId == token.sub değilse, ya da rol izinli değilse).
/// </summary>
public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string errorCode, string message)
        : base(errorCode, message)
    {
    }

    public override int StatusCode => 403;
}
