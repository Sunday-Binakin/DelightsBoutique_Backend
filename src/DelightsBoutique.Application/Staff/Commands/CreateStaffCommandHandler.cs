using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Staff.Dtos;
using DelightsBoutique.Application.Staff.Queries;
using DelightsBoutique.Domain.Entities;
using MediatR;

namespace DelightsBoutique.Application.Staff.Commands;

public class CreateStaffCommandHandler(
    IStaffRepository staff, IPasswordGenerator passwordGenerator, IPasswordHasher passwordHasher, INotificationQueue notifications)
    : IRequestHandler<CreateStaffCommand, CreateStaffResult>
{
    public async Task<CreateStaffResult> Handle(CreateStaffCommand request, CancellationToken ct)
    {
        var password = passwordGenerator.Generate();
        var member = StaffMember.Create(request.Name, request.Email.ToLowerInvariant(), request.Phone, request.Permissions, passwordHasher.Hash(password));

        await staff.AddAsync(member, ct);

        notifications.Enqueue(new NotificationMessage(
            RecipientEmail: member.Email,
            RecipientPhone: null, // staff-account emails are email-only, never WhatsApp
            Subject: "Your Delight's Boutique staff account",
            Body: $"<p>Hi {member.Name},</p><p>An account has been created for you. Your one-time password is <strong>{password}</strong>. Please log in and keep it safe — it will not be shown again.</p>"));

        return new CreateStaffResult(StaffDtoMapper.ToDto(member), password);
    }
}
