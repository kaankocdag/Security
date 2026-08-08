using FluentValidation;

namespace Kaan.SecurityPlatform.Application.Features.Projects.Dtos;

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2048);
        RuleFor(x => x.PrimaryContactEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.PrimaryContactEmail));
    }
}

public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2048);
        RuleFor(x => x.PrimaryContactEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.PrimaryContactEmail));
    }
}
