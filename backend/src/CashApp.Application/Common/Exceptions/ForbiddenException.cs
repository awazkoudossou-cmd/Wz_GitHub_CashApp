namespace CashApp.Application.Common.Exceptions;

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Access denied.") : base(message) { }
}
