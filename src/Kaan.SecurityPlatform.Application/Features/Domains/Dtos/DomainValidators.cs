using FluentValidation;

namespace Kaan.SecurityPlatform.Application.Features.Domains.Dtos;

public sealed class CreateDomainRequestValidator : AbstractValidator<CreateDomainRequest>
{
    public CreateDomainRequestValidator()
    {
        RuleFor(x => x.SecurityProjectId).NotEmpty();
        RuleFor(x => x.HostName).NotEmpty().MaximumLength(253)
            .Matches("^[A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?(\\.[A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?)+$")
            .WithMessage("Geçerli bir domain giriniz. Örn: example.com");
        RuleFor(x => x.Scheme).Must(v => v is "http" or "https").WithMessage("Şema sadece http veya https olabilir.");
        RuleFor(x => x.Port).InclusiveBetween(1, 65535).When(x => x.Port is not null);
    }
}

public sealed class StartVerificationRequestValidator : AbstractValidator<StartVerificationRequest>
{
    public StartVerificationRequestValidator()
    {
        RuleFor(x => x.DomainId).NotEmpty();
    }
}
