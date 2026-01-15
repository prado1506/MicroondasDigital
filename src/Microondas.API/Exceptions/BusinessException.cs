namespace Microondas.API.Exceptions;

public class BusinessException : Exception
{
    public string? Code { get; }
    public BusinessException(string message, string? code = null) : base(message) => Code = code;
    public BusinessException(string message, Exception inner, string? code = null) : base(message, inner) => Code = code;
}