// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlTestCorpusTests.Surfaces.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Test.Kat;
using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Nodes;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

/// <summary>
/// Extends the conformance-corpus coverage beyond <see cref="TomlDocumentReader" /> to the other read and write
/// surfaces: every valid corpus document must also parse through the read-only <see cref="TomlDocument" />, survive a
/// mutable <see cref="TomlNode" /> round-trip through the canonical writer with its pinned values intact, and produce
/// canonical output that is stable across a reparse.
/// </summary>
public sealed partial class TomlTestCorpusTests
{
    /// <summary>
    /// Verifies that every valid v1.0.0 corpus document parses through the read-only <see cref="TomlDocument" />
    /// surface.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidCasesV10), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenCorpusDocumentIsValid_ForDocumentV10_ShouldParse(CorpusKat kat) =>
        AssertDocumentParses(kat);

    /// <summary>
    /// Verifies that every valid v1.1.0 corpus document parses through the read-only <see cref="TomlDocument" />
    /// surface.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidCasesV11), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenCorpusDocumentIsValid_ForDocumentV11_ShouldParse(CorpusKat kat) =>
        AssertDocumentParses(kat);

    /// <summary>
    /// Verifies that every valid v1.0.0 corpus document round-trips through the mutable <see cref="TomlNode" /> tree
    /// and the canonical writer with the values pinned by its JSON expectation intact.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidCasesV10), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenCorpusDocumentIsValid_ForNodeRoundTripV10_ShouldMatchExpectedValues(CorpusKat kat) =>
        AssertNodeRoundTrip(kat);

    /// <summary>
    /// Verifies that every valid v1.1.0 corpus document round-trips through the mutable <see cref="TomlNode" /> tree
    /// and the canonical writer with the values pinned by its JSON expectation intact.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidCasesV11), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenCorpusDocumentIsValid_ForNodeRoundTripV11_ShouldMatchExpectedValues(CorpusKat kat) =>
        AssertNodeRoundTrip(kat);

    /// <summary>
    /// Asserts that a valid corpus document parses through <see cref="TomlDocument.Parse(ReadOnlySpan{byte}, TomlDocumentOptions)" />.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    private static void AssertDocumentParses(CorpusKat kat)
    {
        byte[] toml = File.ReadAllBytes(Path.Combine(CorpusRoot, kat.RelativePath));

        using var document = TomlDocument.Parse(toml, new TomlDocumentOptions { SpecVersion = kat.SpecVersion });

        Assert.AreEqual(TomlValueKind.Table, document.RootElement.ValueKind, kat.RelativePath);
    }

    /// <summary>
    /// Asserts that a valid corpus document survives a node round-trip through the canonical writer: the tree's
    /// canonical bytes must reparse to the case's JSON expectation, and the canonical form must be a fixed point of
    /// write-then-reparse.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    private static void AssertNodeRoundTrip(CorpusKat kat)
    {
        byte[] toml = File.ReadAllBytes(Path.Combine(CorpusRoot, kat.RelativePath));
        string expectationPath = Path.Combine(CorpusRoot, Path.ChangeExtension(kat.RelativePath, ".json"));

        byte[] canonical = ParseNode(toml, kat.SpecVersion).ToUtf8Bytes();

        var reader = new TomlDocumentReader(canonical, new TomlReaderOptions { SpecVersion = kat.SpecVersion });
        Assert.IsTrue(reader.Read(), $"{kat.RelativePath}: the canonical bytes produced no tokens.");
        object actual = BuildValue(ref reader);

        using var expected = JsonDocument.Parse(File.ReadAllBytes(expectationPath));
        AssertMatches(expected.RootElement, actual, kat.RelativePath);

        byte[] restated = ParseNode(canonical, kat.SpecVersion).ToUtf8Bytes();
        CollectionAssert.AreEqual(canonical, restated, $"{kat.RelativePath}: the canonical form is not a fixed point of write-then-reparse.");
    }

    /// <summary>
    /// Parses a document into a mutable node tree under the supplied specification profile.
    /// </summary>
    /// <param name="toml">The UTF-8 TOML bytes.</param>
    /// <param name="version">The specification profile to parse under.</param>
    /// <returns>The root node.</returns>
    private static TomlNode ParseNode(byte[] toml, TomlSpecVersion version)
    {
        var reader = new TomlDocumentReader(toml, new TomlReaderOptions { SpecVersion = version });
        Assert.IsTrue(reader.Read(), "The document produced no tokens.");
        return TomlNode.ReadFrom(ref reader, default);
    }
}
