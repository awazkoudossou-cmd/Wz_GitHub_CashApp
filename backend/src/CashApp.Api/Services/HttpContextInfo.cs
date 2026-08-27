using CashApp.Application.Common.Interfaces;

namespace CashApp.Api.Services;

public class HttpContextInfo : IHttpContextInfo
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextInfo(IHttpContextAccessor accessor) => _accessor = accessor;

    public string? RemoteIp => _accessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
}
