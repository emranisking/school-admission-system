namespace SchoolAdmission.Domain.Exceptions;

public enum ErrorKind
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Unavailable
}

/// <summary>A business-rule failure that maps cleanly to an HTTP error response.</summary>
public class DomainException : Exception
{
    public string Code { get; }
    public ErrorKind Kind { get; }

    public DomainException(string code, string message, ErrorKind kind) : base(message)
    {
        Code = code;
        Kind = kind;
    }

    public static DomainException Validation(string message, string code = "VALIDATION_ERROR")
        => new(code, message, ErrorKind.Validation);

    public static DomainException NotFound(string message = "Resource not found.")
        => new("NOT_FOUND", message, ErrorKind.NotFound);

    public static DomainException Forbidden(string message = "You are not allowed to access this resource.")
        => new("FORBIDDEN", message, ErrorKind.Forbidden);

    public static DomainException Conflict(string code, string message)
        => new(code, message, ErrorKind.Conflict);
}

public class SeatUnavailableException : DomainException
{
    public SeatUnavailableException()
        : base("SEAT_UNAVAILABLE", "No seats are available in this class right now.", ErrorKind.Conflict) { }
}

public class ReservationExpiredException : DomainException
{
    public ReservationExpiredException()
        : base("RESERVATION_EXPIRED", "This reservation has expired and its seat was released.", ErrorKind.Conflict) { }
}
