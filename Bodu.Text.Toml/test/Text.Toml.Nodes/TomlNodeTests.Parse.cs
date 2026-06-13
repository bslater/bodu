// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlNodeTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Kat;

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Verifies that <see cref="TomlNode.Parse(ReadOnlySpan{byte})" /> materializes the correct node kinds from TOML
/// documents, rejects malformed input with <see cref="TomlFormatException" />, and round-trips canonical documents
/// byte-for-byte through <see cref="TomlNode.ToUtf8Bytes" />.
/// </summary>
public partial class TomlNodeTests
{
    /// <summary>
    /// Yields malformed TOML documents that <see cref="TomlNode.Parse(ReadOnlySpan{byte})" /> must reject with
    /// <see cref="TomlFormatException" />.
    /// </summary>
    /// <returns>One <c>[DynamicData]</c> row per malformed-input scenario.</returns>
    public static IEnumerable<object[]> MalformedDocuments()
    {
        static object[] Row(string name, string toml) => [new InvalidKat<string>(name, toml, typeof(TomlFormatException))];

        yield return Row("key without equals", "a 1\n");
        yield return Row("key without value", "a =\n");
        yield return Row("duplicate key", "a = 1\na = 2\n");
        yield return Row("unterminated basic string", "a = \"abc\n");
        yield return Row("invalid escape", "a = \"\\q\"\n");
        yield return Row("integer leading zero", "a = 01\n");
        yield return Row("integer overflow", "a = 9223372036854775808\n");
        yield return Row("float bad exponent", "a = 1e\n");
        yield return Row("month out of range", "a = 2020-13-01\n");
        yield return Row("capitalized true", "a = True\n");
        yield return Row("unterminated table header", "[a\n");
        yield return Row("duplicate table", "[a]\n[a]\n");
        yield return Row("table over value", "a = 1\n[a]\n");
        yield return Row("unterminated array", "a = [1, 2\n");
        yield return Row("array missing comma", "a = [1 2]\n");
        yield return Row("unterminated inline table", "a = {x = 1\n");
        yield return Row("trailing content after value", "a = 1 2\n");
    }

    /// <summary>
    /// Verifies that each malformed TOML document is rejected by <see cref="TomlNode.Parse(ReadOnlySpan{byte})" />
    /// with <see cref="TomlFormatException" />.
    /// </summary>
    /// <param name="kat">The malformed-input scenario under test.</param>
    [TestMethod]
    [DynamicData(nameof(MalformedDocuments), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenInputMalformed_ShouldThrowTomlFormatException(InvalidKat<string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);
        var bytes = Encoding.UTF8.GetBytes(kat.Input);

        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = TomlNode.Parse(bytes);
        });
    }

    /// <summary>
    /// Verifies that parsing an empty document yields an empty <see cref="TomlObject" /> root, because the TOML root
    /// is always a table.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputEmpty_ShouldReturnEmptyObjectRoot()
    {
        var node = TomlNode.Parse(ReadOnlySpan<byte>.Empty);

        TomlObject root = node!.AsObject();
        Assert.AreEqual(0, root.Count);
    }

    /// <summary>
    /// Verifies that parsing materializes each TOML scalar kind as a <see cref="TomlValue" /> of the matching
    /// <see cref="TomlValueKind" />.
    /// </summary>
    /// <param name="toml">The single-member TOML document.</param>
    /// <param name="expectedKind">The expected kind of the member node.</param>
    [TestMethod]
    [DataRow("a = \"x\"\n", TomlValueKind.String)]
    [DataRow("a = 42\n", TomlValueKind.Integer)]
    [DataRow("a = 1.5\n", TomlValueKind.Float)]
    [DataRow("a = true\n", TomlValueKind.Boolean)]
    [DataRow("a = 1979-05-27T07:32:00Z\n", TomlValueKind.OffsetDateTime)]
    [DataRow("a = 1979-05-27T07:32:00\n", TomlValueKind.LocalDateTime)]
    [DataRow("a = 1979-05-27\n", TomlValueKind.LocalDate)]
    [DataRow("a = 07:32:00\n", TomlValueKind.LocalTime)]
    [DataRow("a = [1]\n", TomlValueKind.Array)]
    [DataRow("a = {x = 1}\n", TomlValueKind.Table)]
    public void Parse_WhenMemberKindVaries_ShouldMaterializeMatchingNodeKind(string toml, TomlValueKind expectedKind)
    {
        var node = TomlNode.Parse(Encoding.UTF8.GetBytes(toml));

        Assert.AreEqual(expectedKind, node!.AsObject()["a"]!.GetValueKind());
    }

    /// <summary>
    /// Verifies that parsing canonical documents and re-serializing the resulting trees reproduces the source
    /// byte-for-byte.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="input">The canonical TOML document.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("empty document", "")]
    [DataRow("scalar kinds", "s = \"x\"\ni = 42\nf = 1.5\nb = true\n")]
    [DataRow("date-time kinds", "odt = 1979-05-27T07:32:00Z\nldt = 1979-05-27T07:32:00\nld = 1979-05-27\nlt = 07:32:00\n")]
    [DataRow("integer extremes", "min = -9223372036854775808\nmax = 9223372036854775807\n")]
    [DataRow("arrays", "a = [1, 2, 3]\nnested = [[1], []]\n")]
    [DataRow("sub-table", "name = \"x\"\n\n[sub]\nk = \"v\"\n")]
    public void Parse_WhenInputCanonical_ShouldRoundTripThroughToUtf8Bytes(string testName, string input)
    {
        _ = testName;
        var bytes = Encoding.UTF8.GetBytes(input);

        var roundTripped = TomlNode.Parse(bytes)!.ToUtf8Bytes();

        CollectionAssert.AreEqual(bytes, roundTripped);
    }

    /// <summary>
    /// Verifies that parsing a nested document links every child node to its container, so
    /// <see cref="TomlNode.Root" /> walks back to the parsed root.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputNested_ShouldBuildParentLinks()
    {
        var root = TomlNode.Parse(Encoding.UTF8.GetBytes("list = [1]\n"));

        TomlNode list = root!.AsObject()["list"]!;
        TomlNode leaf = list.AsArray()[0]!;

        Assert.AreSame(root, list.Parent);
        Assert.AreSame(list, leaf.Parent);
        Assert.AreSame(root, leaf.Root);
        Assert.IsNull(root.Parent);
    }

    /// <summary>
    /// Verifies that an array of tables parses into a <see cref="TomlArray" /> of <see cref="TomlObject" /> elements
    /// in document order.
    /// </summary>
    [TestMethod]
    public void Parse_WhenArrayOfTables_ShouldMaterializeArrayOfObjects()
    {
        var root = TomlNode.Parse(Encoding.UTF8.GetBytes("[[p]]\nname = \"a\"\n\n[[p]]\nname = \"b\"\n"));

        TomlArray products = root!.AsObject()["p"]!.AsArray();
        Assert.AreEqual(2, products.Count);
        Assert.AreEqual("a", (string)products[0]!["name"]!);
        Assert.AreEqual("b", (string)products[1]!["name"]!);
    }

    /// <summary>
    /// Verifies that the default parse builds case-sensitive tables, so a lookup differing only in case misses.
    /// </summary>
    [TestMethod]
    public void Parse_WhenOptionsOmitted_ShouldBuildCaseSensitiveTables()
    {
        TomlObject root = TomlNode.Parse(Encoding.UTF8.GetBytes("Name = \"x\"\n"))!.AsObject();

        Assert.IsFalse(root.ContainsKey("name"));
        Assert.IsTrue(root.ContainsKey("Name"));
    }
}
