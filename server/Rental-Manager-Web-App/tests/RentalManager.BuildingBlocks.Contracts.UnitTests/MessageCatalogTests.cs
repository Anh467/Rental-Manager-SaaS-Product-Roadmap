using System.Reflection;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using Xunit;

namespace RentalManager.BuildingBlocks.Contracts.UnitTests;

public sealed class MessageCatalogTests
{
    [Fact]
    public void Every_key_is_either_active_or_deprecated_but_never_both()
    {
        foreach (MessageDefinition definition in MessageCatalog.All)
        {
            bool isActive = MessageCatalog.ActiveKeys.Contains(definition.Key);
            bool isDeprecated = MessageCatalog.DeprecatedKeys.Contains(definition.Key);

            Assert.True(isActive ^ isDeprecated,
                $"{definition.Key} must be exactly one of Active/Deprecated.");
            Assert.Equal(definition.Lifecycle == MessageLifecycle.Active, isActive);
            Assert.Equal(definition.Lifecycle == MessageLifecycle.Deprecated, isDeprecated);
        }
    }

    [Fact]
    public void Catalog_contains_no_duplicate_keys()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (MessageDefinition definition in MessageCatalog.All)
        {
            Assert.True(seen.Add(definition.Key), $"Duplicate key: {definition.Key}");
        }
    }

    [Fact]
    public void Success_catalog_covers_SCS_001_through_SCS_020_and_all_are_active()
    {
        string[] expected = Enumerable.Range(1, 20)
            .Select(number => $"SCS-{number:D3}")
            .ToArray();

        string[] actualSuccessKeys = MessageCatalog.All
            .Where(definition => definition.Kind == MessageKind.Success)
            .Select(definition => definition.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected.OrderBy(key => key, StringComparer.Ordinal), actualSuccessKeys);
        Assert.All(expected, key => Assert.True(MessageCatalog.IsActive(key)));
    }

    [Theory]
    [InlineData("ERR-027")]
    [InlineData("ERR-034")]
    [InlineData("ERR-035")]
    [InlineData("ERR-036")]
    [InlineData("ERR-037")]
    [InlineData("ERR-038")]
    [InlineData("ERR-039")]
    [InlineData("ERR-042")]
    [InlineData("ERR-043")]
    [InlineData("ERR-044")]
    [InlineData("ERR-045")]
    [InlineData("ERR-046")]
    [InlineData("ERR-047")]
    public void Deprecated_error_keys_are_marked_deprecated_and_not_active(string key)
    {
        Assert.True(MessageCatalog.IsDeprecated(key));
        Assert.False(MessageCatalog.IsActive(key));
    }

    [Theory]
    [InlineData("ERR-040")]
    [InlineData("ERR-041")]
    [InlineData("ERR-048")]
    [InlineData("ERR-049")]
    [InlineData("ERR-050")]
    public void Tail_active_error_keys_are_active_and_not_deprecated(string key)
    {
        Assert.True(MessageCatalog.IsActive(key));
        Assert.False(MessageCatalog.IsDeprecated(key));
    }

    [Fact]
    public void Error_catalog_covers_ERR_001_through_ERR_050_with_no_gaps()
    {
        string[] expected = Enumerable.Range(1, 50)
            .Select(number => $"ERR-{number:D3}")
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        string[] actual = MessageCatalog.All
            .Where(definition => definition.Kind == MessageKind.Error)
            .Select(definition => definition.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Error_catalog_has_37_active_and_13_deprecated_keys()
    {
        int activeErrorCount = MessageCatalog.All
            .Count(definition => definition.Kind == MessageKind.Error
                && definition.Lifecycle == MessageLifecycle.Active);
        int deprecatedErrorCount = MessageCatalog.All
            .Count(definition => definition.Kind == MessageKind.Error
                && definition.Lifecycle == MessageLifecycle.Deprecated);

        Assert.Equal(37, activeErrorCount);
        Assert.Equal(13, deprecatedErrorCount);
    }

    /// <summary>
    /// The backend must never emit a deprecated key. Every constant exposed
    /// on <see cref="MessageCode.Success"/> and <see cref="MessageCode.Error"/>
    /// is reflected over and checked against the catalog.
    /// </summary>
    [Fact]
    public void MessageCode_never_exposes_a_deprecated_key()
    {
        IReadOnlyList<string> allConstantValues =
        [
            .. GetConstantStringValues(typeof(MessageCode.Success)),
            .. GetConstantStringValues(typeof(MessageCode.Error))
        ];

        Assert.NotEmpty(allConstantValues);

        foreach (string value in allConstantValues)
        {
            Assert.False(
                MessageCatalog.IsDeprecated(value),
                $"MessageCode exposes deprecated key {value}.");
            Assert.True(
                MessageCatalog.IsActive(value),
                $"MessageCode exposes unknown/inactive key {value}.");
        }
    }

    [Fact]
    public void MessageCode_constants_exist_in_the_catalog_with_matching_kind()
    {
        foreach (string value in GetConstantStringValues(typeof(MessageCode.Success)))
        {
            Assert.True(MessageCatalog.TryGet(value, out MessageDefinition definition));
            Assert.Equal(MessageKind.Success, definition.Kind);
        }

        foreach (string value in GetConstantStringValues(typeof(MessageCode.Error)))
        {
            Assert.True(MessageCatalog.TryGet(value, out MessageDefinition definition));
            Assert.Equal(MessageKind.Error, definition.Kind);
        }
    }

    private static IEnumerable<string> GetConstantStringValues(Type type)
    {
        return type
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!);
    }
}
