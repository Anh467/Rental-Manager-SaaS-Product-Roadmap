using RentalManager.Modules.TenantManagement.Application.Fields;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.Modules.TenantManagement.Core.Enums;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests;

public sealed class FieldInvariantsTests
{
    [Theory]
    [InlineData(EFieldType.Text)]
    [InlineData(EFieldType.Number)]
    [InlineData(EFieldType.Date)]
    [InlineData(EFieldType.Boolean)]
    [InlineData(EFieldType.Selection)]
    [InlineData(EFieldType.MultiSelect)]
    public void Known_catalogue_field_types_are_accepted(EFieldType fieldType)
    {
        Assert.True(FieldInvariants.IsKnownFieldType((int)fieldType));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    [InlineData(-1)]
    public void Unknown_field_type_ids_are_rejected(int fieldTypeId)
    {
        Assert.False(FieldInvariants.IsKnownFieldType(fieldTypeId));
    }

    [Fact]
    public void Selection_and_multi_select_require_options()
    {
        Assert.True(FieldInvariants.RequiresOptions((int)EFieldType.Selection));
        Assert.True(FieldInvariants.RequiresOptions((int)EFieldType.MultiSelect));
        Assert.False(FieldInvariants.RequiresOptions((int)EFieldType.Text));
        Assert.False(FieldInvariants.RequiresOptions((int)EFieldType.Boolean));
        Assert.False(FieldInvariants.RequiresOptions((int)EFieldType.Number));
        Assert.False(FieldInvariants.RequiresOptions((int)EFieldType.Date));
    }

    [Fact]
    public void Object_name_matches_the_shared_message_catalogue()
    {
        Assert.Equal(MessageCode.ObjectName.Field, FieldInvariants.ObjectName);
    }
}
