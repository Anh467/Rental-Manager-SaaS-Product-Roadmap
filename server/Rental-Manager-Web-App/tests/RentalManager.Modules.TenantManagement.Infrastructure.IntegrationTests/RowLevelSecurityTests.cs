using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Tenant isolation on a real SQL Server with the real security policy. Nothing
/// here is mocked, because a mocked predicate would prove nothing about row
/// level security.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class RowLevelSecurityTests
{
    private readonly SqlServerFixture _fixture;

    public RowLevelSecurityTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Another_organization_cannot_read_the_field()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenantA = OrganizationA();
        await using TenantScope tenantB = OrganizationB();

        FieldDto field = await tenantA.CreateTextFieldAsync("room_number");

        ResourceNotFoundException exception =
            await Assert.ThrowsAsync<ResourceNotFoundException>(
                () => tenantB.GetAsync(field.Id));

        Assert.Equal(MessageCode.Error.NotFound, exception.MessageKey);

        Assert.Empty(
            (await tenantB.ListAsync(new GetFieldsRequest())).Items);
    }

    [Fact]
    public async Task Another_organization_cannot_update_or_delete_the_field()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenantA = OrganizationA();
        await using TenantScope tenantB = OrganizationB();

        FieldDto field = await tenantA.CreateTextFieldAsync("room_number");

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => tenantB.UpdateAsync(field.Id, field.ToRenameRequest("Hijacked")));

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => tenantB.DeleteAsync(
                field.Id,
                new DeleteFieldRequest { RowVersion = field.RowVersion }));

        Assert.Equal("room_number", (await tenantA.GetAsync(field.Id)).Name);
        Assert.Null((await tenantA.GetAsync(field.Id)).Description);
    }

    [Fact]
    public async Task Same_key_in_two_organizations_is_allowed()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenantA = OrganizationA();
        await using TenantScope tenantB = OrganizationB();

        FieldDto fromA = await tenantA.CreateTextFieldAsync("room_number");
        FieldDto fromB = await tenantB.CreateTextFieldAsync("room_number");

        Assert.NotEqual(fromA.Id, fromB.Id);
        Assert.Equal("room_number", fromA.Key);
        Assert.Equal("room_number", fromB.Key);
    }

    [Fact]
    public async Task Without_an_organization_context_the_application_fails_closed()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenantA = OrganizationA();

        FieldDto field = await tenantA.CreateTextFieldAsync("room_number");

        await using TenantScope unbound = TenantScope.Unbound(_fixture);
        await using AsyncServiceScope scope = unbound.BeginUnitOfWork();

        var fields = scope.ServiceProvider.GetRequiredService<IOrgFieldRepository>();

        MissingOrganizationContextException exception =
            await Assert.ThrowsAsync<MissingOrganizationContextException>(
                () => fields.GetAsync(field.Id));

        Assert.Equal(
            MessageCode.Error.OrganizationContextMissing,
            exception.MessageKey);
    }

    /// <summary>
    /// The second line of defence: even a caller that reaches the table directly
    /// sees nothing until an organization has been published to the session.
    /// </summary>
    [Fact]
    public async Task Without_a_session_context_the_database_returns_no_row()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenantA = OrganizationA();

        await tenantA.CreateTextFieldAsync("room_number");

        await using SqlConnection connection = await _fixture.OpenConnectionAsync();

        Assert.Equal(
            0,
            await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT_BIG(1) FROM [org].[Field];"));

        await SqlServerFixture.SetOrganizationContextAsync(
            connection,
            TestData.OrganizationA.Id);

        Assert.Equal(
            1,
            await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT_BIG(1) FROM [org].[Field];"));
    }

    /// <summary>
    /// The block predicate stops a write that would stamp a row with another
    /// organization, so the isolation does not depend on application code
    /// remembering to filter.
    /// </summary>
    [Fact]
    public async Task Database_blocks_a_write_that_belongs_to_another_organization()
    {
        await _fixture.ResetOrgDataAsync();

        await using SqlConnection connection =
            await _fixture.OpenConnectionAsync(TestData.OrganizationA.Id);

        SqlException exception = await Assert.ThrowsAsync<SqlException>(
            () => connection.ExecuteAsync(
                """
                INSERT INTO [org].[Field]
                    ([Id], [OrganizationId], [Key], [Name], [FieldTypeId],
                     [CreatedAt], [UpdatedAt])
                VALUES
                    (@Id, @OrganizationId, N'smuggled', N'Smuggled', 1,
                     @Now, @Now);
                """,
                new
                {
                    Id = Guid.CreateVersion7(),
                    OrganizationId = TestData.OrganizationB.Id,
                    Now = DateTimeOffset.UtcNow
                }));

        Assert.Equal(33504, exception.Number);

        Assert.Equal(
            0,
            await _fixture.CountRowsIgnoringSecurityAsync(
                "[org].[Field]",
                "[Key] = N'smuggled'"));
    }

    private TenantScope OrganizationA()
    {
        return TenantScope.For(
            _fixture,
            TestData.OrganizationA.Id,
            TestData.Users.AdministratorAId);
    }

    private TenantScope OrganizationB()
    {
        return TenantScope.For(
            _fixture,
            TestData.OrganizationB.Id,
            TestData.Users.AdministratorBId);
    }
}
