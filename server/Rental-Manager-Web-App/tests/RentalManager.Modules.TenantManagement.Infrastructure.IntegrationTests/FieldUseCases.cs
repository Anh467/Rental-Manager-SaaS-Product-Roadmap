using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Fields.Commands;
using RentalManager.Modules.TenantManagement.Application.Fields.Queries;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
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
            .GetRequiredService<ICommandHandler<CreateFieldCommand, FieldDto>>()
            .HandleAsync(new CreateFieldCommand(request));
    }

    public static Task<FieldDto> CreateTextFieldAsync(
        this TenantScope tenant,
        string key,
        string? name = null)
    {
        return tenant.CreateAsync(new CreateFieldRequest
        {
            Key = key,
            Name = name ?? key,
            FieldTypeId = (int)EFieldType.Text
        });
    }

    public static Task<FieldDto> CreateMultiSelectFieldAsync(
        this TenantScope tenant,
        string key,
        IReadOnlyList<FieldOptionInput> options)
    {
        return tenant.CreateAsync(new CreateFieldRequest
        {
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
            .GetRequiredService<ICommandHandler<UpdateFieldCommand, FieldDto>>()
            .HandleAsync(new UpdateFieldCommand(id, request));
    }

    public static async Task DeleteAsync(
        this TenantScope tenant,
        Guid id,
        DeleteFieldRequest request)
    {
        await using AsyncServiceScope scope = tenant.BeginUnitOfWork();

        await scope.ServiceProvider
            .GetRequiredService<ICommandHandler<DeleteFieldCommand>>()
            .HandleAsync(new DeleteFieldCommand(id, request));
    }

    public static async Task<FieldDto> GetAsync(
        this TenantScope tenant,
        Guid id)
    {
        await using AsyncServiceScope scope = tenant.BeginUnitOfWork();

        return await scope.ServiceProvider
            .GetRequiredService<IQueryHandler<GetFieldQuery, FieldDto>>()
            .HandleAsync(new GetFieldQuery(id));
    }

    public static async Task<PagedResult<FieldDto>> ListAsync(
        this TenantScope tenant,
        GetFieldsRequest request)
    {
        await using AsyncServiceScope scope = tenant.BeginUnitOfWork();

        return await scope.ServiceProvider
            .GetRequiredService<IQueryHandler<GetFieldsQuery, PagedResult<FieldDto>>>()
            .HandleAsync(new GetFieldsQuery(request));
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
            IsActive = field.IsActive,
            RowVersion = field.RowVersion,
            Options = field.Options.Count == 0
                ? null
                : field.Options
                    .Select(option => new FieldOptionInput
                    {
                        Key = option.Key,
                        Name = option.Name,
                        Description = option.Description,
                        DisplayOrder = option.DisplayOrder,
                        IsActive = option.IsActive
                    })
                    .ToArray()
        };
    }
}
