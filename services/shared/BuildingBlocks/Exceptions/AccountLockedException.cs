namespace BuildingBlocks.Exceptions;

/// <summary>
/// Hesap kilitliyken giriş denendiğinde (Core_Principles §10: 5 hatalı giriş → 15 dk kilit).
/// Kalan süre hem mesajda hem <see cref="ResponseData"/> (JSON <c>data.remainingSeconds</c>) olarak döner.
/// </summary>
public sealed class AccountLockedException : DomainException
{
    public AccountLockedException(int remainingSeconds)
        : base("ACCOUNT_LOCKED", $"Hesap kilitli. Kalan sure: {remainingSeconds} saniye.")
    {
        RemainingSeconds = remainingSeconds;
    }

    public int RemainingSeconds { get; }

    public override int StatusCode => 423;

    public override object? ResponseData => new { remainingSeconds = RemainingSeconds };
}
