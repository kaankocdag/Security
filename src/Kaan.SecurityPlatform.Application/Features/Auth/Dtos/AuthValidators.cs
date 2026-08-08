using FluentValidation;

namespace Kaan.SecurityPlatform.Application.Features.Auth.Dtos;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(10)
            .Matches("[A-Z]").WithMessage("Parola en az bir büyük harf içermelidir.")
            .Matches("[a-z]").WithMessage("Parola en az bir küçük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Parola en az bir rakam içermelidir.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Parola en az bir özel karakter içermelidir.");
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.AcceptTerms).Equal(true).WithMessage("Kullanım koşullarını kabul etmelisiniz.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
