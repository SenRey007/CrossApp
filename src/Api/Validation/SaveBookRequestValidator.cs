using Api.Contracts;
using FluentValidation;

namespace Api.Validation;

public sealed class SaveBookRequestValidator : AbstractValidator<SaveBookRequest>
{
    public SaveBookRequestValidator()
    {
        RuleFor(x => x.Isbn)
            .NotEmpty().WithMessage("ISBN обов'язковий.")
            .MaximumLength(20).WithMessage("ISBN занадто довгий (до 20 символів).");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Назва обов'язкова, до 200 символів.")
            .MaximumLength(200).WithMessage("Назва обов'язкова, до 200 символів.");

        RuleFor(x => x.Author)
            .MaximumLength(100).WithMessage("Автор: до 100 символів.");

        // Верхня межа обчислюється під час перевірки, а не один раз при старті.
        RuleFor(x => x.Year)
            .Must(year => year >= 1450 && year <= DateTime.Now.Year)
            .WithMessage(_ => $"Рік має бути в межах 1450..{DateTime.Now.Year}.");
    }
}