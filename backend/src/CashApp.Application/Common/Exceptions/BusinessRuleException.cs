namespace CashApp.Application.Common.Exceptions;

public class BusinessRuleException : AppException
{
    public string Code { get; }

    public BusinessRuleException(string code, string message) : base(message)
    {
        Code = code;
    }
}
