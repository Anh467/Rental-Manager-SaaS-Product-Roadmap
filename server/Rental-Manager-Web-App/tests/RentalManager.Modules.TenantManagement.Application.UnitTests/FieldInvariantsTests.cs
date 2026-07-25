using RentalManager.Modules.TenantManagement.Application.Fields;
using RentalManager.Modules.TenantManagement.Core.Enums;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests;

public sealed class FieldInvariantsTests
{
    [Theory]
    [InlineData("Property")]
    [InlineData("Room")]
    [InlineData("Person")]
    [InlineData("Document")]
    public void Supported_target_entity_types_are_accepted(string targetEntityType)
    {
        Assert.True(FieldInvariants.IsSupportedTargetEntityType(targetEntityType));
    }

    [Theory]
    [InlineData("property")]
    [InlineData("Invoice")]
    [InlineData("")]
    [InlineData(null)]
    public void Unknown_target_entity_types_are_rejected(string? targetEntityType)
    {
        Assert.False(FieldInvariants.IsSupportedTargetEntityType(targetEntityType));
    }

    [Theory]
    [InlineData(EFieldType.Text)]
    [InlineData(EFieldType.Number)]
    [InlineData(EFieldType.Boolean)]
    [InlineData(EFieldType.MultiSelect)]
    public void Mvp_field_types_are_supported(EFieldType fieldType)
    {
        Assert.True(FieldInvariants.IsSupportedFieldType((int)fieldType));
    }

    [Theory]
    [InlineData(EFieldType.Date)]
    [InlineData(EFieldType.Selection)]
    public void Field_types_outside_the_mvp_are_rejected(EFieldType fieldType)
    {
        Assert.False(FieldInvariants.IsSupportedFieldType((int)fieldType));
    }

    [Fact]
    public void Unknown_field_type_ids_are_rejected()
    {
        Assert.False(FieldInvariants.IsSupportedFieldType(0));
        Assert.False(FieldInvariants.IsSupportedFieldType(99));
    }

    [Fact]
    public void Only_multi_select_owns_options()
    {
        Assert.True(FieldInvariants.RequiresOptions((int)EFieldType.MultiSelect));
        Assert.False(FieldInvariants.RequiresOptions((int)EFieldType.Text));
        Assert.False(FieldInvariants.RequiresOptions((int)EFieldType.Boolean));
    }

    [Fact]
    public void Lock_resource_name_is_scoped_to_organization_and_target()
    {
        var organizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        string first = FieldInvariants.LockResourceName(organizationId, "Room");
        string second = FieldInvariants.LockResourceName(organizationId, "Property");
        string other = FieldInvariants.LockResourceName(Guid.NewGuid(), "Room");

        Assert.NotEqual(first, second);
        Assert.NotEqual(first, other);
        Assert.Equal(first, FieldInvariants.LockResourceName(organizationId, "Room"));
    }

    [Fact]
    public void Audit_summary_contains_only_configuration_properties()
    {
        string summary = FieldInvariants.DescribeConfiguration(
            "Room number",
            isRequired: true,
            isActive: true,
            isPrimaryDisplayField: false);

        Assert.Equal(
            "Name=Room number; IsRequired=True; IsActive=True; " +
            "IsPrimaryDisplayField=False",
            summary);
    }
}
