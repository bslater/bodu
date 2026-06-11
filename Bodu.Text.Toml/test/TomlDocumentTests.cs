// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Toml.Document;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the read-only TOML document object model (<see cref="TomlDocument" />, <see cref="TomlElement" />, and
/// <see cref="TomlProperty" />), the <see cref="System.Text.Json.JsonDocument" /> analogue for TOML.
/// </summary>
[TestClass]
public partial class TomlDocumentTests
{
    /// <summary>
    /// A representative document exercising a header table, nested keys, an array, and one of each scalar kind.
    /// </summary>
    private const string SampleToml =
        "title = \"Example\"\n" +
        "count = 42\n" +
        "ratio = 1.5\n" +
        "enabled = true\n" +
        "created = 1979-05-27T07:32:00Z\n" +
        "stamp = 1979-05-27T07:32:00\n" +
        "day = 1979-05-27\n" +
        "moment = 07:32:00\n" +
        "ports = [8000, 8001, 8002]\n" +
        "\n" +
        "[owner]\n" +
        "name = \"Tom\"\n" +
        "\n" +
        "[owner.address]\n" +
        "city = \"Anytown\"\n";

    /// <summary>
    /// Verifies that a document with a table, nested keys, an array, and one of each scalar kind parses, exposes a
    /// table root, and reads every scalar back as its CLR type.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Parse_WhenDocumentHasTablesArraysAndScalars_ShouldExposeReadableModel()
    {
        using var document = TomlDocument.Parse(SampleToml);

        TomlElement root = document.RootElement;
        Assert.AreEqual(TomlValueKind.Table, root.ValueKind);

        Assert.AreEqual("Example", root.GetProperty("title").GetString());
        Assert.AreEqual(42L, root.GetProperty("count").GetInt64());
        Assert.AreEqual(1.5d, root.GetProperty("ratio").GetDouble());
        Assert.IsTrue(root.GetProperty("enabled").GetBoolean());
        Assert.AreEqual(
            new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero),
            root.GetProperty("created").GetDateTimeOffset());
        Assert.AreEqual(new DateTime(1979, 5, 27, 7, 32, 0), root.GetProperty("stamp").GetDateTime());
        Assert.AreEqual(new DateOnly(1979, 5, 27), root.GetProperty("day").GetDateOnly());
        Assert.AreEqual(new TimeOnly(7, 32, 0), root.GetProperty("moment").GetTimeOnly());
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.GetProperty(string)" /> walks nested tables to reach a deeply located key.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenKeyIsNested_ShouldResolveThroughChildTables()
    {
        using var document = TomlDocument.Parse(SampleToml);

        TomlElement city = document.RootElement
            .GetProperty("owner")
            .GetProperty("address")
            .GetProperty("city");

        Assert.AreEqual(TomlValueKind.String, city.ValueKind);
        Assert.AreEqual("Anytown", city.GetString());
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.EnumerateObject" /> yields every key/value pair of a table in stored order.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenTableHasPairs_ShouldYieldNamesAndValues()
    {
        using var document = TomlDocument.Parse("[owner]\nname = \"Tom\"\nrole = \"admin\"\n");

        TomlElement owner = document.RootElement.GetProperty("owner");

        List<string> names = [];
        List<string> values = [];
        foreach (TomlProperty property in owner.EnumerateObject())
        {
            names.Add(property.Name);
            values.Add(property.Value.GetString());
        }

        CollectionAssert.AreEqual(new[] { "name", "role" }, names);
        CollectionAssert.AreEqual(new[] { "Tom", "admin" }, values);
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.EnumerateArray" /> yields each array element in order.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenArrayHasElements_ShouldYieldElementsInOrder()
    {
        using var document = TomlDocument.Parse("ports = [8000, 8001, 8002]\n");

        TomlElement ports = document.RootElement.GetProperty("ports");

        List<long> values = [];
        foreach (TomlElement element in ports.EnumerateArray())
            values.Add(element.GetInt64());

        CollectionAssert.AreEqual(new[] { 8000L, 8001L, 8002L }, values);
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.GetArrayLength" /> reports the element count and the indexer returns the
    /// element at a given position.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAccessingArrayElement_ShouldReturnElementAtPosition()
    {
        using var document = TomlDocument.Parse("ports = [8000, 8001, 8002]\n");

        TomlElement ports = document.RootElement.GetProperty("ports");

        Assert.AreEqual(3, ports.GetArrayLength());
        Assert.AreEqual(8000L, ports[0].GetInt64());
        Assert.AreEqual(8002L, ports[2].GetInt64());
    }

    /// <summary>
    /// Verifies that an array whose elements are tables (a TOML array-of-tables) is navigable element-by-element.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenArrayOfTables_ShouldExposeEachTable()
    {
        using var document = TomlDocument.Parse(
            "[[products]]\nname = \"Hammer\"\n\n[[products]]\nname = \"Nail\"\n");

        TomlElement products = document.RootElement.GetProperty("products");
        Assert.AreEqual(TomlValueKind.Array, products.ValueKind);
        Assert.AreEqual(2, products.GetArrayLength());

        Assert.AreEqual("Hammer", products[0].GetProperty("name").GetString());
        Assert.AreEqual("Nail", products[1].GetProperty("name").GetString());
    }

    /// <summary>
    /// Verifies that invoking a typed scalar accessor on an element of a different kind throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenElementIsString_ShouldThrowInvalidOperationException()
    {
        using var document = TomlDocument.Parse("title = \"Example\"\n");

        TomlElement title = document.RootElement.GetProperty("title");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = title.GetInt64();
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.GetArrayLength" /> throws <see cref="InvalidOperationException" /> when the
    /// element is not an array.
    /// </summary>
    [TestMethod]
    public void GetArrayLength_WhenElementIsTable_ShouldThrowInvalidOperationException()
    {
        using var document = TomlDocument.Parse("[owner]\nname = \"Tom\"\n");

        TomlElement owner = document.RootElement.GetProperty("owner");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = owner.GetArrayLength();
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.EnumerateArray" /> throws <see cref="InvalidOperationException" /> when the
    /// element is not an array.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenElementIsString_ShouldThrowInvalidOperationException()
    {
        using var document = TomlDocument.Parse("title = \"Example\"\n");

        TomlElement title = document.RootElement.GetProperty("title");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = title.EnumerateArray();
        });
    }

    /// <summary>
    /// Verifies that requesting a property absent from a table throws <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenPropertyAbsent_ShouldThrowKeyNotFoundException()
    {
        using var document = TomlDocument.Parse("title = \"Example\"\n");

        TomlElement root = document.RootElement;

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = root.GetProperty("missing");
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.TryGetProperty(string, out TomlElement)" /> returns
    /// <see langword="false" /> for an absent property.
    /// </summary>
    [TestMethod]
    public void TryGetProperty_WhenPropertyAbsent_ShouldReturnFalse()
    {
        using var document = TomlDocument.Parse("title = \"Example\"\n");

        bool found = document.RootElement.TryGetProperty("missing", out TomlElement value);

        Assert.IsFalse(found);
        Assert.AreEqual(default, value);
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.TryGetProperty(string, out TomlElement)" /> returns <see langword="true" />
    /// and the matching value for a present property.
    /// </summary>
    [TestMethod]
    public void TryGetProperty_WhenPropertyPresent_ShouldReturnTrueAndValue()
    {
        using var document = TomlDocument.Parse("title = \"Example\"\n");

        bool found = document.RootElement.TryGetProperty("title", out TomlElement value);

        Assert.IsTrue(found);
        Assert.AreEqual("Example", value.GetString());
    }

    /// <summary>
    /// Verifies that indexing an array beyond its bounds throws <see cref="ArgumentOutOfRangeException" />,
    /// reporting the indexer's <c>index</c> parameter name.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenArrayIndexOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        using var document = TomlDocument.Parse("ports = [8000, 8001]\n");

        TomlElement ports = document.RootElement.GetProperty("ports");

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = ports[2];
        }, "index");
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.ToString" /> renders a scalar's value and a container's kind name.
    /// </summary>
    [TestMethod]
    public void ToString_WhenElementIsScalarOrContainer_ShouldRenderValueOrKindName()
    {
        using var document = TomlDocument.Parse("title = \"Example\"\nports = [1]\n");

        TomlElement root = document.RootElement;

        Assert.AreEqual("Example", root.GetProperty("title").ToString());
        Assert.AreEqual(nameof(TomlValueKind.Array), root.GetProperty("ports").ToString());
        Assert.AreEqual(nameof(TomlValueKind.Table), root.ToString());
    }

    /// <summary>
    /// Verifies that accessing an element after the owning document is disposed throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void ValueKind_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = TomlDocument.Parse("title = \"Example\"\n");
        TomlElement root = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = root.ValueKind;
        });
    }

    /// <summary>
    /// Verifies that disposing a document more than once is a no-op.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var document = TomlDocument.Parse("title = \"Example\"\n");

        document.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Verifies that parsing a span of UTF-8 bytes yields a document equivalent to parsing the same text.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGivenUtf8Bytes_ShouldReadValues()
    {
        byte[] utf8 = Encoding.UTF8.GetBytes("name = \"x\"\nport = 8080\n");

        using var document = TomlDocument.Parse(utf8.AsSpan());

        Assert.AreEqual("x", document.RootElement.GetProperty("name").GetString());
        Assert.AreEqual(8080L, document.RootElement.GetProperty("port").GetInt64());
    }

    /// <summary>
    /// Verifies that parsing malformed TOML throws <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsMalformed_ShouldThrowTomlFormatException()
    {
        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            using var document = TomlDocument.Parse("key = = 1\n");
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlDocument.Parse(string)" /> rejects a <see langword="null" /> string with
    /// <see cref="ArgumentNullException" /> and <c>ParamName</c> <c>toml</c>.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStringIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            using var document = TomlDocument.Parse((string)null!);
        }, "toml");
    }
}
