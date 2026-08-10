using System.Text.RegularExpressions;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using Xunit;

namespace RentalManager.BuildingBlocks.Contracts.UnitTests;

/// <summary>
/// Cross-stack parity: canonical <see cref="MessageCatalog"/> must match the
/// frontend TypeScript catalog and both locale trees. The C# catalog remains
/// the single source of truth; this test fails CI when TS/locales drift.
/// </summary>
public sealed class MessageCatalogParityTests
{
    [Fact]
    public void Frontend_message_catalog_matches_backend_canonical_catalog()
    {
        string catalogPath = ResolvePath(
            Path.Combine(
                "client",
                "rental-manager-web-app",
                "src",
                "api",
                "client",
                "message-catalog.ts"));

        string source = File.ReadAllText(catalogPath);

        HashSet<string> frontendActiveSuccess = ParseConstArray(source, "ACTIVE_SUCCESS_MESSAGE_KEYS");
        HashSet<string> frontendDeprecatedSuccess = ParseConstArray(source, "DEPRECATED_SUCCESS_MESSAGE_KEYS");
        HashSet<string> frontendActiveError = ParseConstArray(source, "ACTIVE_ERROR_MESSAGE_KEYS");
        HashSet<string> frontendDeprecatedError = ParseConstArray(source, "DEPRECATED_ERROR_MESSAGE_KEYS");

        HashSet<string> backendActiveSuccess = MessageCatalog.All
            .Where(d => d.Kind == MessageKind.Success && d.Lifecycle == MessageLifecycle.Active)
            .Select(d => d.Key)
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> backendDeprecatedSuccess = MessageCatalog.All
            .Where(d => d.Kind == MessageKind.Success && d.Lifecycle == MessageLifecycle.Deprecated)
            .Select(d => d.Key)
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> backendActiveError = MessageCatalog.All
            .Where(d => d.Kind == MessageKind.Error && d.Lifecycle == MessageLifecycle.Active)
            .Select(d => d.Key)
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> backendDeprecatedError = MessageCatalog.All
            .Where(d => d.Kind == MessageKind.Error && d.Lifecycle == MessageLifecycle.Deprecated)
            .Select(d => d.Key)
            .ToHashSet(StringComparer.Ordinal);

        AssertSetsEqual("active success", backendActiveSuccess, frontendActiveSuccess);
        AssertSetsEqual("deprecated success", backendDeprecatedSuccess, frontendDeprecatedSuccess);
        AssertSetsEqual("active error", backendActiveError, frontendActiveError);
        AssertSetsEqual("deprecated error", backendDeprecatedError, frontendDeprecatedError);
    }

    [Fact]
    public void Locale_files_match_backend_canonical_catalog()
    {
        HashSet<string> successKeys = MessageCatalog.All
            .Where(d => d.Kind == MessageKind.Success)
            .Select(d => d.Key)
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> errorKeys = MessageCatalog.All
            .Where(d => d.Kind == MessageKind.Error)
            .Select(d => d.Key)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string locale in new[] { "en", "vi" })
        {
            AssertSetsEqual(
                $"{locale}/success.json",
                successKeys,
                ReadJsonObjectKeys(ResolvePath(
                    Path.Combine(
                        "client",
                        "rental-manager-web-app",
                        "src",
                        "locales",
                        locale,
                        "success.json"))));

            AssertSetsEqual(
                $"{locale}/error.json",
                errorKeys,
                ReadJsonObjectKeys(ResolvePath(
                    Path.Combine(
                        "client",
                        "rental-manager-web-app",
                        "src",
                        "locales",
                        locale,
                        "error.json"))));
        }
    }

    [Theory]
    [InlineData("SCS-999")]
    [InlineData("ERR-999")]
    [InlineData("ERR-XYZ")]
    public void Unknown_keys_are_not_in_canonical_catalog(string key)
    {
        Assert.False(MessageCatalog.IsActive(key));
        Assert.False(MessageCatalog.IsDeprecated(key));
        Assert.False(MessageCatalog.TryGet(key, out _));
    }

    private static HashSet<string> ParseConstArray(string source, string constName)
    {
        Match match = Regex.Match(
            source,
            $@"export const {Regex.Escape(constName)}\s*=\s*\[(?<body>[\s\S]*?)\]\s*as const;",
            RegexOptions.CultureInvariant);

        Assert.True(match.Success, $"Could not find {constName} in message-catalog.ts");

        return Regex.Matches(match.Groups["body"].Value, "\"(?<key>[^\"]+)\"")
            .Select(m => m.Groups["key"].Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> ReadJsonObjectKeys(string path)
    {
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static void AssertSetsEqual(
        string label,
        IReadOnlySet<string> expected,
        IReadOnlySet<string> actual)
    {
        string[] missing = expected.Except(actual, StringComparer.Ordinal).OrderBy(k => k).ToArray();
        string[] extra = actual.Except(expected, StringComparer.Ordinal).OrderBy(k => k).ToArray();

        Assert.True(
            missing.Length == 0 && extra.Length == 0,
            $"{label} catalog drift. missing=[{string.Join(", ", missing)}] extra=[{string.Join(", ", extra)}]");
    }

    private static string ResolvePath(params string[] relativeSegments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(
                new[] { directory.FullName }.Concat(relativeSegments).ToArray());

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not resolve path '{string.Join('/', relativeSegments)}'.");
    }
}
