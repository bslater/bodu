// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentTests.Elements.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Text.Toml.Document;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the <see cref="TomlElement" /> accessor surface: each kind-specific getter rejects elements of every other
/// kind with <see cref="InvalidOperationException" />, scalar extremes decode correctly, and positional and named
/// navigation skips nested subtrees.
/// </summary>
public partial class TomlDocumentTests
{
    /// <summary>
    /// A document whose root members cover all ten <see cref="TomlValueKind" /> members, keyed by kind name.
    /// </summary>
    private const string AllKindsToml =
        "String = \"x\"\n" +
        "Integer = 42\n" +
        "Float = 1.5\n" +
        "Boolean = true\n" +
        "OffsetDateTime = 1979-05-27T07:32:00Z\n" +
        "LocalDateTime = 1979-05-27T07:32:00\n" +
        "LocalDate = 1979-05-27\n" +
        "LocalTime = 07:32:00\n" +
        "Array = [1]\n" +
        "Table = {x = 1}\n";

    /// <summary>
    /// Verifies that each typed accessor throws <see cref="InvalidOperationException" /> when invoked on an element of
    /// any other kind, sweeping the full accessor-by-kind mismatch matrix.
    /// </summary>
    /// <param name="accessor">The name of the kind-specific accessor under test.</param>
    /// <param name="elementKind">The kind of the element the accessor is invoked on.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(AccessorKindMismatches))]
    public void Accessors_WhenElementKindMismatched_ShouldThrowInvalidOperationException(string accessor, string elementKind)
    {
        using var document = TomlDocument.Parse(AllKindsToml);
        TomlElement element = document.RootElement.GetProperty(elementKind);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            Invoke(element, accessor);
        });
    }

    /// <summary>
    /// Yields one row per accessor/kind combination in which the accessor must reject the element.
    /// </summary>
    /// <returns>Rows of accessor name and mismatched element-kind name.</returns>
    public static IEnumerable<object[]> AccessorKindMismatches()
    {
        var requiredKind = new Dictionary<string, string>
        {
            ["GetString"] = "String",
            ["GetInt64"] = "Integer",
            ["GetDouble"] = "Float",
            ["GetBoolean"] = "Boolean",
            ["GetDateTimeOffset"] = "OffsetDateTime",
            ["GetDateTime"] = "LocalDateTime",
            ["GetDateOnly"] = "LocalDate",
            ["GetTimeOnly"] = "LocalTime",
            ["GetArrayLength"] = "Array",
            ["EnumerateArray"] = "Array",
            ["EnumerateObject"] = "Table",
            ["GetProperty"] = "Table",
            ["TryGetProperty"] = "Table",
            ["ArrayIndexer"] = "Array",
        };

        string[] kinds =
        [
            "String", "Integer", "Float", "Boolean", "OffsetDateTime", "LocalDateTime", "LocalDate", "LocalTime",
            "Array", "Table",
        ];

        foreach (KeyValuePair<string, string> entry in requiredKind)
        {
            foreach (string kind in kinds)
            {
                if (!string.Equals(kind, entry.Value, StringComparison.Ordinal))
                    yield return [entry.Key, kind];
            }
        }
    }

    /// <summary>
    /// Invokes the named kind-specific accessor on the supplied element, discarding the result.
    /// </summary>
    /// <param name="element">The element to invoke the accessor on.</param>
    /// <param name="accessor">The accessor name from <see cref="AccessorKindMismatches" />.</param>
    private static void Invoke(TomlElement element, string accessor)
    {
        switch (accessor)
        {
            case "GetString": _ = element.GetString(); break;
            case "GetInt64": _ = element.GetInt64(); break;
            case "GetDouble": _ = element.GetDouble(); break;
            case "GetBoolean": _ = element.GetBoolean(); break;
            case "GetDateTimeOffset": _ = element.GetDateTimeOffset(); break;
            case "GetDateTime": _ = element.GetDateTime(); break;
            case "GetDateOnly": _ = element.GetDateOnly(); break;
            case "GetTimeOnly": _ = element.GetTimeOnly(); break;
            case "GetArrayLength": _ = element.GetArrayLength(); break;
            case "EnumerateArray": _ = element.EnumerateArray(); break;
            case "EnumerateObject": _ = element.EnumerateObject(); break;
            case "GetProperty": _ = element.GetProperty("x"); break;
            case "TryGetProperty": _ = element.TryGetProperty("x", out _); break;
            default: _ = element[0]; break;
        }
    }

    /// <summary>
    /// Verifies that each scalar accessor returns the decoded CLR value for an element of its own kind.
    /// </summary>
    [TestMethod]
    public void Accessors_WhenElementKindMatches_ShouldReturnDecodedValues()
    {
        using var document = TomlDocument.Parse(AllKindsToml);
        TomlElement root = document.RootElement;

        Assert.AreEqual("x", root.GetProperty("String").GetString());
        Assert.AreEqual(42L, root.GetProperty("Integer").GetInt64());
        Assert.AreEqual(1.5d, root.GetProperty("Float").GetDouble());
        Assert.IsTrue(root.GetProperty("Boolean").GetBoolean());
        Assert.AreEqual(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero), root.GetProperty("OffsetDateTime").GetDateTimeOffset());
        Assert.AreEqual(new DateTime(1979, 5, 27, 7, 32, 0), root.GetProperty("LocalDateTime").GetDateTime());
        Assert.AreEqual(new DateOnly(1979, 5, 27), root.GetProperty("LocalDate").GetDateOnly());
        Assert.AreEqual(new TimeOnly(7, 32, 0), root.GetProperty("LocalTime").GetTimeOnly());
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.GetDateTime" /> reports <see cref="DateTimeKind.Unspecified" /> for a
    /// local date-time, which carries no offset or zone relation.
    /// </summary>
    [TestMethod]
    public void GetDateTime_WhenLocalDateTime_ShouldReportUnspecifiedKind()
    {
        using var document = TomlDocument.Parse("stamp = 1979-05-27T07:32:00\n");

        Assert.AreEqual(DateTimeKind.Unspecified, document.RootElement.GetProperty("stamp").GetDateTime().Kind);
    }

    /// <summary>
    /// Verifies that the 64-bit integer extremes decode through <see cref="TomlElement.GetInt64" />.
    /// </summary>
    /// <param name="input">The TOML integer document.</param>
    /// <param name="expected">The expected decoded value.</param>
    [TestMethod]
    [DataRow("a = 9223372036854775807\n", long.MaxValue)]
    [DataRow("a = -9223372036854775808\n", long.MinValue)]
    [DataRow("a = 0\n", 0L)]
    public void GetInt64_WhenElementIsInt64Extreme_ShouldReturnOriginalValue(string input, long expected)
    {
        using var document = TomlDocument.Parse(input);

        Assert.AreEqual(expected, document.RootElement.GetProperty("a").GetInt64());
    }

    /// <summary>
    /// Verifies that the special float values decode through <see cref="TomlElement.GetDouble" />.
    /// </summary>
    [TestMethod]
    public void GetDouble_WhenElementIsSpecialFloat_ShouldReturnSpecialValue()
    {
        using var document = TomlDocument.Parse("pi = inf\nni = -inf\nn = nan\n");
        TomlElement root = document.RootElement;

        Assert.AreEqual(double.PositiveInfinity, root.GetProperty("pi").GetDouble());
        Assert.AreEqual(double.NegativeInfinity, root.GetProperty("ni").GetDouble());
        Assert.IsTrue(double.IsNaN(root.GetProperty("n").GetDouble()));
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.GetProperty(string)" /> throws <see cref="ArgumentNullException" /> with
    /// <c>ParamName</c> <c>propertyName</c> when the property name is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenPropertyNameIsNull_ShouldThrowArgumentNullException()
    {
        using var document = TomlDocument.Parse("a = 1\n");
        TomlElement root = document.RootElement;

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = root.GetProperty(null!);
        }, "propertyName");
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.TryGetProperty(string, out TomlElement)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>propertyName</c> when the property name is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryGetProperty_WhenPropertyNameIsNull_ShouldThrowArgumentNullException()
    {
        using var document = TomlDocument.Parse("a = 1\n");
        TomlElement root = document.RootElement;

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = root.TryGetProperty(null!, out _);
        }, "propertyName");
    }

    /// <summary>
    /// Verifies that the positional indexer throws <see cref="ArgumentOutOfRangeException" /> when the index is
    /// negative, reporting the indexer's <c>index</c> parameter name.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        using var document = TomlDocument.Parse("a = [1]\n");
        TomlElement array = document.RootElement.GetProperty("a");

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = array[-1];
        }, "index");
    }

    /// <summary>
    /// Verifies that property-name matching is ordinal and case-sensitive.
    /// </summary>
    [TestMethod]
    public void TryGetProperty_WhenNameDiffersByCase_ShouldReturnFalse()
    {
        using var document = TomlDocument.Parse("Name = 1\n");

        Assert.IsFalse(document.RootElement.TryGetProperty("name", out _));
        Assert.IsTrue(document.RootElement.TryGetProperty("Name", out _));
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.GetProperty(string)" /> matches a quoted key containing multi-byte UTF-8
    /// characters.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenKeyIsMultiByteUtf8_ShouldMatch()
    {
        using var document = TomlDocument.Parse("\"héllo\" = 1\n");

        Assert.AreEqual(1L, document.RootElement.GetProperty("héllo").GetInt64());
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.GetArrayLength" /> reports zero for an empty array.
    /// </summary>
    [TestMethod]
    public void GetArrayLength_WhenArrayEmpty_ShouldReturnZero()
    {
        using var document = TomlDocument.Parse("a = []\n");

        Assert.AreEqual(0, document.RootElement.GetProperty("a").GetArrayLength());
    }

    /// <summary>
    /// Verifies that positional indexing skips whole nested subtrees, so elements after a nested inline table or
    /// array are located correctly.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenArrayContainsMixedKinds_ShouldSkipNestedSubtrees()
    {
        using var document = TomlDocument.Parse("a = [1, {x = \"b\"}, [2], \"spam\"]\n");
        TomlElement array = document.RootElement.GetProperty("a");

        Assert.AreEqual(4, array.GetArrayLength());
        Assert.AreEqual(1L, array[0].GetInt64());
        Assert.AreEqual("b", array[1].GetProperty("x").GetString());
        Assert.AreEqual(2L, array[2][0].GetInt64());
        Assert.AreEqual("spam", array[3].GetString());
    }

    /// <summary>
    /// Verifies that named navigation after a nested container sibling resolves correctly, because property lookup
    /// skips each preceding value's whole subtree.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenPrecedingSiblingIsContainer_ShouldSkipSubtree()
    {
        using var document = TomlDocument.Parse("a = [1, 2]\nb = {x = 1}\nc = 3\n");

        Assert.AreEqual(3L, document.RootElement.GetProperty("c").GetInt64());
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.ToString" /> renders each scalar kind in its invariant round-trip text and
    /// containers as the kind name.
    /// </summary>
    /// <param name="key">The root member to render.</param>
    /// <param name="expected">The expected rendering.</param>
    [TestMethod]
    [DataRow("String", "x")]
    [DataRow("Integer", "42")]
    [DataRow("Float", "1.5")]
    [DataRow("Boolean", "true")]
    [DataRow("OffsetDateTime", "1979-05-27T07:32:00.0000000+00:00")]
    [DataRow("LocalDateTime", "1979-05-27T07:32:00.0000000")]
    [DataRow("LocalDate", "1979-05-27")]
    [DataRow("LocalTime", "07:32:00.0000000")]
    [DataRow("Array", "Array")]
    [DataRow("Table", "Table")]
    public void ToString_WhenElementKindVaries_ShouldRenderKindSpecificText(string key, string expected)
    {
        using var document = TomlDocument.Parse(AllKindsToml);

        Assert.AreEqual(expected, document.RootElement.GetProperty(key).ToString());
    }

    /// <summary>
    /// Verifies that <see cref="TomlProperty.ToString" /> returns the property name.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCalledOnProperty_ShouldReturnName()
    {
        using var document = TomlDocument.Parse("title = \"x\"\n");

        foreach (TomlProperty property in document.RootElement.EnumerateObject())
            Assert.AreEqual("title", property.ToString());
    }
}
