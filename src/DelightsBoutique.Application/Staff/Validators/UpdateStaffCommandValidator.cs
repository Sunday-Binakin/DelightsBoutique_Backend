using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Staff.Commands;
using FluentValidation;

namespace DelightsBoutique.Application.Staff.Validators;

public class UpdateStaffCommandValidator : AbstractValidator<UpdateStaffCommand>
{
    public UpdateStaffCommandValidator(IStaffRepository staff)
    {
        RuleFor(x => x.Name).NotEmpty().When(x => x.Name is not null);
        RuleFor(x => x.Email).EmailAddress()
            .MustAsync(async (command, email, ct) => email is null || !await staff.EmailExistsAsync(email!, command.Id, ct))
            .WithMessage(x => $"A staff member with email '{x.Email}' already exists.")
            .When(x => x.Email is not null);
        RuleFor(x => x.Permissions).NotEmpty().WithMessage("At least one permission is required.").When(x => x.Permissions is not null);
    }
}
