using Microsoft.SqlServer.Dac;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Deploys the database project's DACPAC into the test database. The schema
/// under test is therefore the same artefact that is deployed to a real
/// environment, so these tests cannot pass against a schema that only exists in
/// test code.
/// </summary>
internal static class DacpacDeployer
{
    private const string DacpacFileName = "RentalManager.Database.SQLServer.dacpac";

    public static void Deploy(string connectionString, string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        using var package = DacPackage.Load(
            ResolveDacpacPath(),
            DacSchemaModelStorageType.Memory);

        var services = new DacServices(connectionString);

        services.Deploy(
            package,
            databaseName,
            upgradeExisting: true,
            new DacDeployOptions
            {
                CreateNewDatabase = true,
                BlockOnPossibleDataLoss = false,
                IncludeCompositeObjects = true,
                // The project has no cross-database reference, so an unresolved
                // reference is a real defect rather than something to ignore.
                AllowIncompatiblePlatform = true
            });
    }

    /// <summary>
    /// Finds the most recently built DACPAC. The database project is a build
    /// dependency of this test project, so it is guaranteed to exist; which
    /// configuration produced it depends on how the tests were invoked.
    /// </summary>
    private static string ResolveDacpacPath()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable(
            "RENTAL_MANAGER_TEST_DACPAC_PATH");

        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return File.Exists(fromEnvironment)
                ? fromEnvironment
                : throw new FileNotFoundException(
                    "RENTAL_MANAGER_TEST_DACPAC_PATH does not point at a file.",
                    fromEnvironment);
        }

        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var projectDirectory = new DirectoryInfo(
                Path.Combine(
                    directory.FullName,
                    "src",
                    "RentalManager.Database.SQLServer",
                    "bin"));

            if (projectDirectory.Exists)
            {
                FileInfo? dacpac = projectDirectory
                    .GetFiles(DacpacFileName, SearchOption.AllDirectories)
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .FirstOrDefault();

                if (dacpac is not null)
                {
                    return dacpac.FullName;
                }
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"'{DacpacFileName}' was not found. Build " +
            "src/RentalManager.Database.SQLServer first, or set " +
            "RENTAL_MANAGER_TEST_DACPAC_PATH.");
    }
}
