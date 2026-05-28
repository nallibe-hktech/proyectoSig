using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SIG.API.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _provider;
    public ValidationFilter(IServiceProvider provider) { _provider = provider; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null) continue;
            var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
            var validator = _provider.GetService(validatorType) as IValidator;
            if (validator is null) continue;
            var ctx = new ValidationContext<object>(arg);
            var result = await validator.ValidateAsync(ctx);
            if (!result.IsValid)
            {
                var errors = result.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                var pd = new ValidationProblemDetails(errors) { Status = 400, Title = "Validation failed" };
                pd.Extensions["code"] = "validation_error";
                context.Result = new BadRequestObjectResult(pd);
                return;
            }
        }
        await next();
    }
}
