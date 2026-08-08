using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kaan.SecurityPlatform.Api.Infrastructure.Filters;

public sealed class FluentValidationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments)
        {
            if (argument.Value is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.Value.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is IValidator validator)
            {
                var validationContext = new ValidationContext<object>(argument.Value);
                var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
                if (!result.IsValid)
                {
                    var errors = result.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                    context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors)
                    {
                        Title = "Doğrulama hatası",
                        Type = "https://kaansecurity.local/errors/validation_error"
                    });
                    return;
                }
            }
        }

        await next();
    }
}
