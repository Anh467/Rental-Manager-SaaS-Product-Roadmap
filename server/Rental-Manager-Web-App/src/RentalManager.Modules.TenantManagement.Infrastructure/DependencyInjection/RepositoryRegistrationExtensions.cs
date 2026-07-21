using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace RentalManager.Modules.TenantManagement.Infrastructure.DependencyInjection;

internal static class RepositoryRegistrationExtensions
{
    public static IServiceCollection AddRepositories(
        this IServiceCollection services,
        Assembly contractAssembly,
        Assembly implementationAssembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(contractAssembly);
        ArgumentNullException.ThrowIfNull(implementationAssembly);

        Type[] repositoryContracts = contractAssembly
            .DefinedTypes
            .Where(type =>
                type.IsInterface &&
                !type.IsGenericTypeDefinition &&
                type.Name.EndsWith("Repository", StringComparison.Ordinal))
            .Select(type => type.AsType())
            .ToArray();

        Type[] repositoryImplementations = implementationAssembly
            .DefinedTypes
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                !type.IsGenericTypeDefinition)
            .Select(type => type.AsType())
            .ToArray();

        foreach (Type repositoryContract in repositoryContracts)
        {
            Type[] matchingImplementations = repositoryImplementations
                .Where(repositoryContract.IsAssignableFrom)
                .ToArray();

            if (matchingImplementations.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No implementation was found for repository contract " +
                    $"'{repositoryContract.FullName}'.");
            }

            if (matchingImplementations.Length > 1)
            {
                string implementationNames = string.Join(
                    ", ",
                    matchingImplementations.Select(type => type.FullName));

                throw new InvalidOperationException(
                    $"Multiple implementations were found for repository contract " +
                    $"'{repositoryContract.FullName}': {implementationNames}.");
            }

            services.AddScoped(
                repositoryContract,
                matchingImplementations[0]);
        }

        return services;
    }
}
