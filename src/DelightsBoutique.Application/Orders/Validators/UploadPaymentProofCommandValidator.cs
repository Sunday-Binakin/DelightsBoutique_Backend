using DelightsBoutique.Application.Orders.Commands;
using FluentValidation;

namespace DelightsBoutique.Application.Orders.Validators;

public class UploadPaymentProofCommandValidator : AbstractValidator<UploadPaymentProofCommand>
{
    public UploadPaymentProofCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.FileBytes).Must(b => b.Length > 0).WithMessage("file is required.");
    }
}
