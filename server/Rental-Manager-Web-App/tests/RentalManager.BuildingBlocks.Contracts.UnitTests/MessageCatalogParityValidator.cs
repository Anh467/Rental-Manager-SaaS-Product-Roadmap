using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace RentalManager.BuildingBlocks.Contracts.UnitTests;

/// <summary>
/// Validates catalog key lists for duplicates and set parity. Duplicate checks
/// always run on the original ordered list before any HashSet conversion.
/// </summary>
internal static class MessageCatalogParityValidator
{
    public static void AssertNoDuplicates(string sourceLabel, IReadOnlyList<string> keys)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLabel);
        ArgumentNullException.ThrowIfNull(keys);

        string[] duplicates = keys
            .GroupBy(key => key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} (x{group.Count()})")
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            duplicates.Length == 0,
            $"{sourceLabel} contains duplicate key(s): [{string.Join(", ", duplicates)}]");
    }

    public static void AssertSetsEqual(
        string label,
        IReadOnlyCollection<string> expected,
        IReadOnlyCollection<string> actual)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        AssertNoDuplicates($"{label} expected", expected.ToList());
        AssertNoDuplicates($"{label} actual", actual.ToList());

        HashSet<string> expectedSet = expected.ToHashSet(StringComparer.Ordinal);
        HashSet<string> actualSet = actual.ToHashSet(StringComparer.Ordinal);

        string[] missing = expectedSet.Except(actualSet, StringComparer.Ordinal).OrderBy(k => k).ToArray();
        string[] extra = actualSet.Except(expectedSet, StringComparer.Ordinal).OrderBy(k => k).ToArray();

        Assert.True(
            missing.Length == 0 && extra.Length == 0,
            $"{label} catalog drift. missing=[{string.Join(", ", missing)}] extra=[{string.Join(", ", extra)}]");
    }

    public static void AssertUnknownPrefixedKeysRejected(
        string sourceLabel,
        IReadOnlyCollection<string> knownKeys,
        IReadOnlyCollection<string> candidateKeys)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLabel);
        ArgumentNullException.ThrowIfNull(knownKeys);
        ArgumentNullException.ThrowIfNull(candidateKeys);

        HashSet<string> known = knownKeys.ToHashSet(StringComparer.Ordinal);
        string[] unknown = candidateKeys
            .Where(key =>
                (key.StartsWith("SCS-", StringComparison.Ordinal) ||
                 key.StartsWith("ERR-", StringComparison.Ordinal)) &&
                !known.Contains(key))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            unknown.Length == 0,
            $"{sourceLabel} contains unknown prefixed key(s): [{string.Join(", ", unknown)}]");
    }

    public static IReadOnlyList<string> ParseConstArrayKeys(string source, string constName)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(constName);

        Match match = Regex.Match(
            source,
            $@"export const {Regex.Escape(constName)}\s*=\s*\[(?<body>[\s\S]*?)\]\s*as const;",
            RegexOptions.CultureInvariant);

        Assert.True(match.Success, $"Could not find {constName} in TypeScript catalog source.");

        return Regex.Matches(match.Groups["body"].Value, "\"(?<key>[^\"]+)\"")
            .Select(m => m.Groups["key"].Value)
            .ToList();
    }

    /// <summary>
    /// Reads JSON object property names with <see cref="Utf8JsonReader"/> before
    /// any Dictionary/JsonDocument mapping can drop duplicate keys.
    /// </summary>
    public static IReadOnlyList<string> ReadJsonObjectKeysPreservingDuplicates(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        byte[] utf8 = File.ReadAllBytes(path);
        if (utf8.Length >= 3 && utf8[0] == 0xEF && utf8[1] == 0xBB && utf8[2] == 0xBF)
        {
            utf8 = utf8.AsSpan(3).ToArray();
        }

        var keys = new List<string>();
        var reader = new Utf8JsonReader(utf8, new JsonReaderOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        });

        Assert.True(reader.Read(), $"JSON file '{path}' is empty.");
        Assert.Equal(JsonTokenType.StartObject, reader.TokenType);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            Assert.Equal(JsonTokenType.PropertyName, reader.TokenType);
            keys.Add(reader.GetString() ?? string.Empty);
            reader.Skip();
        }

        return keys;
    }

    public static string MaterializeFixture(string relativeFixturePath, string contents)
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "rm-catalog-parity-fixtures",
            Guid.CreateVersion7().ToString("N"));

        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, relativeFixturePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return path;
    }
}
