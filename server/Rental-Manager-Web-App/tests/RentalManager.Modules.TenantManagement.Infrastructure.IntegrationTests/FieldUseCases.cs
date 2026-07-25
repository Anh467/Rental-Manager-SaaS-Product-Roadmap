using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Fields;
using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;
using RentalManager.Modules.TenantManagement.Core.Enums;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Runs one field use case per unit of work, which is how the API behaves: a
/// request gets a scope, the scope gets a connection, and the connection is
/// returned to the pool afterwards.
/// </summary>
internal static class FieldUseCases
{
    public static async Task<FieldDto> CreateAsync(
        this TenantScope tenant,
        CreateFieldRequest request)
    {
        await using AsyncServiceScope scope = tenant.BeginUnitOfWork();

        return await scope.ServiceProvider
            .GetRequiredService<IFieldService>()
            .CreateFieldAsync(request);
    }

    public static Task<FieldDto> CreateTextFieldAsync(
        this TenantScope tenant,
        string key,
        string targetEntityType = TestData.TargetEntityType.Room,
        string? name = null,
        int displayOrder = 0)
    {
        return tenant.CreateAsync(new CreateFieldRequest
        {
            TargetEntityType = targetEntityType,
            Key = key,
            Name = name ?? key,
            FieldTypeId = (int)EFieldType.Text,
            DisplayOrder = displayOrder
        });
    }

    public static Task<FieldDto> CreateMultiSelectFieldAsync(
        this TenantScope tenant,
        string key,
        IReadOnlyList<FieldOptionInput> options,
        string targetEntityType = TestData.TargetEntityType.Room)
    {
        return tenant.CreateAsync(new CreateFieldRequest
        {
            TargetEntityType = targetEntityType,
            Key = key,
            Name = key,
            FieldTypeId = (int)EFieldType.MultiSelect,
            Options = options
        });
    }

    public static async Task<FieldDto> UpdateAsync(
        this TenantScope tenant,
        Guid id,
        UpdateFieldRequest request)
    {
        await using AsyncServiceScope scope = tenant.BeginUnitOfWork();

        return await scope.ServiceProvider
            .GetRequiredService<IFieldService>()
            .UpdateFieldAsync(id, request);
    }

    public static async Task DeleteAsync(
        this TenantScope tenant,
        Guid id,
        DeleteFieldRequest request)
    {
        await using AsyncServiceScope scope = tenant.BeginUnitOfWork();

        await scope.ServiceProvider
            .GetRequiredService<IFieldService>()
            .DeleteFieldAsync(id, request);
    }

    public static async Task<FieldDto> GetAsync(
        this TenantScope tenant,
        Guid id)
    {
        await using AsyncServiceScope scope = tenant.BeginUnitOfWork();

        return await scope.ServiceProvider
            .GetRequiredService<IFieldService>()
            .GetFieldAsync(id);
    }

    public static async Task<PagedResult<FieldDto>> ListAsync(
        this TenantScope tenant,
        GetFieldsRequest request)
    {
        await using AsyncServiceScope scope = tenant.BeginUnitOfWork();

        return await scope.ServiceProvider
            .GetRequiredService<IFieldService>()
            .GetFieldsAsync(request);
    }

    /// <summary>
    /// Rename that keeps every other property as it is, so a test only has to
    /// state what it is actually changing.
    /// </summary>
    public static UpdateFieldRequest ToRenameRequest(
        this FieldDto field,
        string name)
    {
        ArgumentNullException.ThrowIfNull(field);

        return new UpdateFieldRequest
        {
            Name = name,
            Description = field.Description,
            IsRequired = field.IsRequired,
            IsPrimaryDisplayField = field.IsPrimaryDisplayField,
            IsActive = field.IsActive,
            DisplayOrder = field.DisplayOrder,
            RowVersion = field.RowVersion,
            Options = field.Options.Count == 0
                ? null
                : field.Options
                    .Select(option => new FieldOptionInput
                    {
                        Key = option.Key,
                        Name = option.Name,
                        DisplayOrder = option.DisplayOrder,
                        IsActive = option.IsActive
                    })
                    .ToArray()
        };
    }
}
