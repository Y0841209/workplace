namespace WorkplaceBooking.SharedKernel.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }
    public object? Details { get; }

    public DomainException(string message, string code = "DOMAIN_ERROR", object? details = null)
        : base(message)
    {
        Code = code;
        Details = details;
    }
}

public class ValidationException : Exception
{
    public IReadOnlyList<ValidationError> Errors { get; }

    public ValidationException(IReadOnlyList<ValidationError> errors)
        : base("Validation failed")
    {
        Errors = errors;
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

public readonly record struct ValidationError(string Property, string Message);