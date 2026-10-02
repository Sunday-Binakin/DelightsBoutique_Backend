using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Staff.Commands;
using FluentValidation;

namespace DelightsBoutique.Application.Staff.Validators;

public class CreateStaffCommandValidator : AbstractValidator<CreateStaffCommand>
{
    public CreateStaffCommandValidator(IStaffRepository staff)
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress()
            .MustAsync(async (email, ct) => !await staff.EmailExistsAsync(email, null, ct))
            .WithMessage(x => $"A staff member with email '{x.Email}' already exists.");
        RuleFor(x => x.Permissions).NotEmpty().WithMessage("At least one permission is required.");
    }
}
