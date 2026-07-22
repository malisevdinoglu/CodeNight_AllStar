namespace BuildingBlocks.Exceptions;

/// <summary>E-posta/şifre veya OTP hatalı olduğunda (401).</summary>
public sealed class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException(string errorCode, string message)
        : base(errorCode, message)
    {
    }

    public override int StatusCode => 401;
}
