using DelightsBoutique.Application.Reports.Queries;
using FluentValidation;

namespace DelightsBoutique.Application.Reports.Validators;

public class GetReportsQueryValidator : AbstractValidator<GetReportsQuery>
{
    public GetReportsQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.To >= x.From)
            .WithMessage("'to' must not be earlier than 'from'.")
            .OverridePropertyName("to");
    }
}
