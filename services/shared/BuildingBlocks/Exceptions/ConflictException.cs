namespace BuildingBlocks.Exceptions;

/// <summary>
/// Çakışma durumları için (Core_Principles §5 örneği: "ikinci kez puanlama" → 409).
/// </summary>
public sealed class ConflictException : DomainException
{
    public ConflictException(string errorCode, string message, IReadOnlyList<string>? details = null)
        : base(errorCode, message, details)
    {
    }

    public override int StatusCode => 409;
}
