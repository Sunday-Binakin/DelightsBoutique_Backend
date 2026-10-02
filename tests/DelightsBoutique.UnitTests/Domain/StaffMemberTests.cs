using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using FluentAssertions;

namespace DelightsBoutique.UnitTests.Domain;

public class StaffMemberTests
{
    [Fact]
    public void Create_SetsFieldsAndDefaultsActiveTrue()
    {
        var member = StaffMember.Create("Jane Staff", "jane@delightsboutique.local", "0241234567", [Permission.ManageOrders], "hashed");

        member.Name.Should().Be("Jane Staff");
        member.Email.Should().Be("jane@delightsboutique.local");
        member.Phone.Should().Be("0241234567");
        member.Permissions.Should().ContainSingle().Which.Should().Be(Permission.ManageOrders);
        member.Active.Should().BeTrue();
        member.PasswordHash.Should().Be("hashed");
    }

    [Fact]
    public void UpdateProfile_WithAllFieldsNull_LeavesEverythingUnchanged()
    {
        var member = StaffMember.Create("Jane", "jane@x.com", "0241234567", [Permission.ManageOrders], "hash");
        var originalUpdatedAt = member.UpdatedAt;

        member.UpdateProfile(null, null, null, null, null);

        member.Name.Should().Be("Jane");
        member.Email.Should().Be("jane@x.com");
        member.Active.Should().BeTrue();
        member.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateProfile_WithSpecificFields_OnlyUpdatesThose()
    {
        var member = StaffMember.Create("Jane", "jane@x.com", "0241234567", [Permission.ManageOrders], "hash");

        member.UpdateProfile("Janet", null, null, null, false);

        member.Name.Should().Be("Janet");
        member.Email.Should().Be("jane@x.com"); // unchanged
        member.Active.Should().BeFalse();
    }

    [Fact]
    public void UpdateProfile_WithPermissions_ReplacesPermissionList()
    {
        var member = StaffMember.Create("Jane", "jane@x.com", null, [Permission.ManageOrders], "hash");

        member.UpdateProfile(null, null, null, [Permission.ManageCustomers, Permission.ManageContent], null);

        member.Permissions.Should().BeEquivalentTo([Permission.ManageCustomers, Permission.ManageContent]);
    }

    [Fact]
    public void ResetPassword_SetsNewHash()
    {
        var member = StaffMember.Create("Jane", "jane@x.com", null, [Permission.ManageOrders], "old-hash");

        member.ResetPassword("new-hash");

        member.PasswordHash.Should().Be("new-hash");
    }
}
