using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using Xunit;
using FieldOptionEntity = RentalManager.Modules.TenantManagement.Domain.Entities.Org.FieldOption;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Race conditions and SQL error classification, exercised with independent
/// connections so the database is the thing being tested rather than a lock held
/// inside one process.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class FieldConcurrencyTests
{
    private readonly SqlServerFixture _fixture;

    public FieldConcurrencyTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Two_concurrent_creates_of_the_same_key_leave_exactly_one_field()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        // Two units of work, therefore two connections and two transactions.
        Task<FieldDto> first = Task.Run(
            () => tenant.CreateTextFieldAsync("room_number"));

        Task<FieldDto> second = Task.Run(
            () => tenant.CreateTextFieldAsync("room_number"));

        Exception?[] outcomes =
        [
            await CaptureAsync(first),
            await CaptureAsync(second)
        ];

        Assert.Single(outcomes, outcome => outcome is null);

        DomainException loser = Assert.IsAssignableFrom<DomainException>(
            Assert.Single(outcomes, outcome => outcome is not null));

        Assert.Equal(MessageCode.Error.AlreadyExists, loser.MessageKey);

        Assert.Equal(
            1,
            await _fixture.CountRowsIgnoringSecurityAsync(
                "[org].[Field]",
                "[NormalizedKey] = N'ROOM_NUMBER'"));
    }

    [Fact]
    public async Task Two_concurrent_creates_of_different_keys_both_succeed()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        Task<FieldDto> first = Task.Run(
            () => tenant.CreateTextFieldAsync("room_number"));

        Task<FieldDto> second = Task.Run(
            () => tenant.CreateTextFieldAsync("floor"));

        FieldDto[] created = await Task.WhenAll(first, second);

        Assert.Equal(2, created.Length);

        // The scope lock serialises them, so exactly one can have become primary.
        Assert.Single(created, field => field.IsPrimaryDisplayField);
    }

    [Fact]
    public async Task Duplicate_option_key_is_recognised_as_a_duplicate()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto field = await tenant.CreateMultiSelectFieldAsync(
            "amenities",
            [new FieldOptionInput { Key = "wifi", Name = "Wi-Fi" }]);

        await using AsyncServiceScope scope = tenant.BeginUnitOfWork();

        var options = scope.ServiceProvider
            .GetRequiredService<IFieldOptionRepository>();

        DuplicateResourceException exception =
            await Assert.ThrowsAsync<DuplicateResourceException>(
                () => options.InsertAsync(NewOption(field.Id, "wifi")));

        Assert.Equal(MessageCode.Error.AlreadyExists, exception.MessageKey);
    }

    /// <summary>
    /// A foreign key violation is a different error number, so it must reach the
    /// caller unchanged rather than being reported as a duplicate.
    /// </summary>
    [Fact]
    public async Task Foreign_key_violation_is_not_translated_into_a_duplicate()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();
        await using AsyncServiceScope scope = tenant.BeginUnitOfWork();

        var options = scope.ServiceProvider
            .GetRequiredService<IFieldOptionRepository>();

        SqlException exception = await Assert.ThrowsAsync<SqlException>(
            () => options.InsertAsync(NewOption(Guid.CreateVersion7(), "wifi")));

        Assert.Equal(547, exception.Number);
    }

    private static FieldOptionEntity NewOption(Guid fieldId, string key)
    {
        return new FieldOptionEntity
        {
            Id = Guid.CreateVersion7(),
            FieldId = fieldId,
            Key = key,
            NormalizedKey = key.ToUpperInvariant(),
            Name = key
        };
    }

    private static async Task<Exception?> CaptureAsync<TResult>(Task<TResult> task)
    {
        try
        {
            await task;
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private TenantScope OrganizationA()
    {
        return TenantScope.For(
            _fixture,
            TestData.OrganizationA.Id,
            TestData.Users.AdministratorAId);
    }
}
