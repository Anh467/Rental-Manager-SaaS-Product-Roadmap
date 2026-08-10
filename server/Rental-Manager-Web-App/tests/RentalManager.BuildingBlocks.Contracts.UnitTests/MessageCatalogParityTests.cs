using RentalManager.BuildingBlocks.Contracts.Messaging;
using Xunit;

namespace RentalManager.BuildingBlocks.Contracts.UnitTests;

/// <summary>
/// Cross-stack parity: canonical <see cref="MessageCatalog"/> must match the
/// frontend TypeScript catalog and both locale trees. Duplicate keys are
/// detected on the original lists before any HashSet conversion.
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

        IReadOnlyList<string> frontendActiveSuccess =
            MessageCatalogParityValidator.ParseConstArrayKeys(source, "ACTIVE_SUCCESS_MESSAGE_KEYS");
        IReadOnlyList<string> frontendDeprecatedSuccess =
            MessageCatalogParityValidator.ParseConstArrayKeys(source, "DEPRECATED_SUCCESS_MESSAGE_KEYS");
        IReadOnlyList<string> frontendActiveError =
            MessageCatalogParityValidator.ParseConstArrayKeys(source, "ACTIVE_ERROR_MESSAGE_KEYS");
        IReadOnlyList<string> frontendDeprecatedError =
            MessageCatalogParityValidator.ParseConstArrayKeys(source, "DEPRECATED_ERROR_MESSAGE_KEYS");

        IReadOnlyList<string> backendActiveSuccess = MessageCatalog.All
            .Where(d => d.Kind == MessageKind.Success && d.Lifecycle == MessageLifecycle.Active)
            .Select(d => d.Key)
            .ToList();

        IReadOnlyList<string> backendDeprecatedSuccess = MessageCatalog.All
            .Where(d => d.Kind == MessageKind.Success && d.Lifecycle == MessageLifecycle.Deprecated)
            .Select(d => d.Key)
            .ToList();

        IReadOnlyList<string> backendActiveError = MessageCatalog.All
            .Where(d => d.Kind == MessageKind.Error && d.Lifecycle == MessageLifecycle.Active)
            .Select(d => d.Key)
            .ToList();

        IReadOnlyList<string> backendDeprecatedError = MessageCatalog.All
            .Where(d => d.Kind == MessageKind.Error && d.Lifecycle == MessageLifecycle.Deprecated)
            .Select(d => d.Key)
            .ToList();

        MessageCatalogParityValidator.AssertNoDuplicates(
            "canonical MessageCatalog",
            MessageCatalog.All.Select(d => d.Key).ToList());

        MessageCatalogParityValidator.AssertSetsEqual(
            "active success",
            backendActiveSuccess,
            frontendActiveSuccess);
        MessageCatalogParityValidator.AssertSetsEqual(
            "deprecated success",
            backendDeprecatedSuccess,
            frontendDeprecatedSuccess);
        MessageCatalogParityValidator.AssertSetsEqual(
            "active error",
            backendActiveError,
            frontendActiveError);
        MessageCatalogParityValidator.AssertSetsEqual(
            "deprecated error",
            backendDeprecatedError,
            frontendDeprecatedError);
    }

    [Fact]
    public void Locale_files_match_backend_canonical_catalog()
    {
        IReadOnlyList<string> successKeys = MessageCatalog.All
            .Where(d => d.Kind == MessageKind.Success)
            .Select(d => d.Key)
            .ToList();

        IReadOnlyList<string> errorKeys = MessageCatalog.All
            .Where(d => d.Kind == MessageKind.Error)
            .Select(d => d.Key)
            .ToList();

        MessageCatalogParityValidator.AssertNoDuplicates("canonical success keys", successKeys);
        MessageCatalogParityValidator.AssertNoDuplicates("canonical error keys", errorKeys);

        foreach (string locale in new[] { "en", "vi" })
        {
            string successPath = ResolvePath(
                Path.Combine(
                    "client",
                    "rental-manager-web-app",
                    "src",
                    "locales",
                    locale,
                    "success.json"));
            string errorPath = ResolvePath(
                Path.Combine(
                    "client",
                    "rental-manager-web-app",
                    "src",
                    "locales",
                    locale,
                    "error.json"));

            IReadOnlyList<string> localeSuccess =
                MessageCatalogParityValidator.ReadJsonObjectKeysPreservingDuplicates(successPath);
            IReadOnlyList<string> localeError =
                MessageCatalogParityValidator.ReadJsonObjectKeysPreservingDuplicates(errorPath);

            MessageCatalogParityValidator.AssertNoDuplicates($"{locale}/success.json", localeSuccess);
            MessageCatalogParityValidator.AssertNoDuplicates($"{locale}/error.json", localeError);

            MessageCatalogParityValidator.AssertSetsEqual(
                $"{locale}/success.json",
                successKeys,
                localeSuccess);
            MessageCatalogParityValidator.AssertSetsEqual(
                $"{locale}/error.json",
                errorKeys,
                localeError);
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

    [Fact]
    public void Fixture_duplicate_in_canonical_list_is_detected_before_set()
    {
        var keys = new List<string> { "ERR-001", "ERR-002", "ERR-001" };

        Exception error = Assert.ThrowsAny<Exception>(
            () => MessageCatalogParityValidator.AssertNoDuplicates("canonical fixture", keys));

        Assert.Contains("canonical fixture", error.Message, StringComparison.Ordinal);
        Assert.Contains("ERR-001", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Fixture_duplicate_in_locale_json_is_detected_before_object_mapping()
    {
        string path = MessageCatalogParityValidator.MaterializeFixture(
            "duplicate-locale.json",
            """
            {
              "ERR-001": "one",
              "ERR-002": "two",
              "ERR-001": "duplicate"
            }
            """);

        IReadOnlyList<string> keys =
            MessageCatalogParityValidator.ReadJsonObjectKeysPreservingDuplicates(path);

        Assert.Equal(3, keys.Count);

        Exception error = Assert.ThrowsAny<Exception>(
            () => MessageCatalogParityValidator.AssertNoDuplicates("fixture/duplicate-locale.json", keys));

        Assert.Contains("fixture/duplicate-locale.json", error.Message, StringComparison.Ordinal);
        Assert.Contains("ERR-001", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Fixture_duplicate_in_frontend_catalog_is_detected_before_set()
    {
        const string source = """
            export const ACTIVE_ERROR_MESSAGE_KEYS = [
              "ERR-001",
              "ERR-002",
              "ERR-001",
            ] as const;
            """;

        IReadOnlyList<string> keys =
            MessageCatalogParityValidator.ParseConstArrayKeys(source, "ACTIVE_ERROR_MESSAGE_KEYS");

        Exception error = Assert.ThrowsAny<Exception>(
            () => MessageCatalogParityValidator.AssertNoDuplicates(
                "fixture/message-catalog.ts ACTIVE_ERROR_MESSAGE_KEYS",
                keys));

        Assert.Contains("fixture/message-catalog.ts", error.Message, StringComparison.Ordinal);
        Assert.Contains("ERR-001", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Fixture_missing_key_fails_parity()
    {
        Exception error = Assert.ThrowsAny<Exception>(
            () => MessageCatalogParityValidator.AssertSetsEqual(
                "fixture/missing",
                ["ERR-001", "ERR-002"],
                ["ERR-001"]));

        Assert.Contains("missing=[ERR-002]", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Fixture_extra_key_fails_parity()
    {
        Exception error = Assert.ThrowsAny<Exception>(
            () => MessageCatalogParityValidator.AssertSetsEqual(
                "fixture/extra",
                ["ERR-001"],
                ["ERR-001", "ERR-999"]));

        Assert.Contains("extra=[ERR-999]", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Fixture_unknown_prefixed_key_is_rejected()
    {
        Exception error = Assert.ThrowsAny<Exception>(
            () => MessageCatalogParityValidator.AssertUnknownPrefixedKeysRejected(
                "fixture/unknown",
                knownKeys: ["ERR-001", "SCS-001"],
                candidateKeys: ["ERR-001", "ERR-999", "SCS-001"]));

        Assert.Contains("ERR-999", error.Message, StringComparison.Ordinal);
        Assert.Contains("fixture/unknown", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Fixture_fully_valid_catalog_passes()
    {
        string path = MessageCatalogParityValidator.MaterializeFixture(
            "valid-locale.json",
            """
            {
              "ERR-001": "one",
              "ERR-002": "two"
            }
            """);

        IReadOnlyList<string> localeKeys =
            MessageCatalogParityValidator.ReadJsonObjectKeysPreservingDuplicates(path);

        MessageCatalogParityValidator.AssertNoDuplicates("fixture/valid-locale.json", localeKeys);
        MessageCatalogParityValidator.AssertSetsEqual(
            "fixture/valid",
            ["ERR-001", "ERR-002"],
            localeKeys);
        MessageCatalogParityValidator.AssertUnknownPrefixedKeysRejected(
            "fixture/valid",
            knownKeys: ["ERR-001", "ERR-002"],
            candidateKeys: localeKeys);
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
