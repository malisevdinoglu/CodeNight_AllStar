namespace BuildingBlocks.Exceptions;

/// <summary>Kayıt bulunamadığında (404).</summary>
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string errorCode, string message)
        : base(errorCode, message)
    {
    }

    public override int StatusCode => 404;
}
