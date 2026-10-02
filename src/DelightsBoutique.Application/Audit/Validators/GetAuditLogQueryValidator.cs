using DelightsBoutique.Application.Audit.Queries;
using FluentValidation;

namespace DelightsBoutique.Application.Audit.Validators;

public class GetAuditLogQueryValidator : AbstractValidator<GetAuditLogQuery>
{
    public GetAuditLogQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}
