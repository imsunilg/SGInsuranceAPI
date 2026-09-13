using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SGInsurance.Application.Common;

namespace SGInsurance.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult Ok<T>(T data, string? message = null) =>
        base.Ok(ApiResponse<T>.Ok(data, message));

    protected static async Task ValidateAsync<T>(IValidator<T> validator, T instance)
    {
        var result = await validator.ValidateAsync(instance);
        if (!result.IsValid) throw new ValidationException(result.Errors);
    }

    protected Guid CurrentUserId =>
        Guid.TryParse(User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : Guid.Empty;
}
