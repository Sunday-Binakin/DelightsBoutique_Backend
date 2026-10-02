using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Staff.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Staff.Commands;

public class ResetStaffPasswordCommandHandler(IStaffRepository staff, IPasswordGenerator passwordGenerator, IPasswordHasher passwordHasher)
    : IRequestHandler<ResetStaffPasswordCommand, ResetPasswordResult?>
{
    public async Task<ResetPasswordResult?> Handle(ResetStaffPasswordCommand request, CancellationToken ct)
    {
        var member = await staff.GetByIdAsync(request.Id, ct);
        if (member is null) return null;

        var password = passwordGenerator.Generate();
        member.ResetPassword(passwordHasher.Hash(password));
        await staff.UpdateAsync(member, ct);

        return new ResetPasswordResult(password);
    }
}
