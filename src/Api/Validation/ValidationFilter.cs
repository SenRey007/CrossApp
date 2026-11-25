using FluentValidation;
using FluentValidation.Results;

namespace Api.Validation;

public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        T? model = context.Arguments.OfType<T>().FirstOrDefault();
        if (model is null)
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["Request"] = ["Тіло запиту обов'язкове."] });

        ValidationResult result = await validator.ValidateAsync(model, context.HttpContext.RequestAborted);
        if (!result.IsValid)
            return TypedResults.ValidationProblem(result.ToDictionary());

        return await next(context); // валідно: викликаємо сам обробник
    }
}