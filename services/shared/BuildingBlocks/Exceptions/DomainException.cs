namespace BuildingBlocks.Exceptions;

/// <summary>
/// Domain kuralı ihlal edildiğinde fırlatılır (Core_Principles §5: "kural dışı state
/// geçişi" → 422). <see cref="GlobalExceptionMiddleware"/> bunu yakalayıp
/// <c>{SERVIS}_{StatusCode}_{ErrorCode}</c> formatında bir hata gövdesine çevirir.
/// Varsayılan durum kodu 422'dir; 409 gereken senaryolar için <see cref="ConflictException"/> kullanılır.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string errorCode, string message, IReadOnlyList<string>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details ?? Array.Empty<string>();
    }

    /// <summary>Hata kodunun {SEBEP} kısmı, örn. "INVALID_TRANSITION".</summary>
    public string ErrorCode { get; }

    public IReadOnlyList<string> Details { get; }

    /// <summary>HTTP durum kodu — sade int (BuildingBlocks, Microsoft.AspNetCore.Http.StatusCodes'a bağımlı olmasın diye).</summary>
    public virtual int StatusCode => 422;

    /// <summary>
    /// Bazı hatalar (örn. 423 hesap kilidi → remainingSeconds) hata gövdesinin
    /// <c>data</c> alanında ek bilgi taşımak zorundadır. Varsayılan null (mevcut davranış korunur).
    /// </summary>
    public virtual object? ResponseData => null;
}
