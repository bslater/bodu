// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.Keys.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Verifies that a bare key composed of letters, digits, underscores, and hyphens surfaces verbatim.
    /// </summary>
    [TestMethod]
    public void Read_WhenBareKeyHasAllowedCharacters_ShouldSurfaceKeyVerbatim()
    {
        Utf8TomlReader reader = Create("A-z_0-9 = 1\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "A-z_0-9");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that a purely numeric bare key is permitted and surfaces as its digit string.
    /// </summary>
    [TestMethod]
    public void Read_WhenBareKeyIsNumeric_ShouldSurfaceAsString()
    {
        Utf8TomlReader reader = Create("1234 = 1\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "1234");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that a basic-quoted key may contain characters illegal in a bare key, such as spaces.
    /// </summary>
    [TestMethod]
    public void Read_WhenQuotedBasicKey_ShouldSurfaceDecodedKey()
    {
        Utf8TomlReader reader = Create("\"needs quoting\" = 1\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "needs quoting");
        ExpectToken(ref reader, TomlTokenType.Integer);
    }

    /// <summary>
    /// Verifies that a literal-quoted key preserves its content without escape processing.
    /// </summary>
    [TestMethod]
    public void Read_WhenQuotedLiteralKey_ShouldSurfaceKeyVerbatim()
    {
        Utf8TomlReader reader = Create("'quoted\\key' = 1\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "quoted\\key");
        ExpectToken(ref reader, TomlTokenType.Integer);
    }

    /// <summary>
    /// Verifies that an empty quoted key is permitted and surfaces as an empty string.
    /// </summary>
    [TestMethod]
    public void Read_WhenQuotedKeyIsEmpty_ShouldSurfaceEmptyKey()
    {
        Utf8TomlReader reader = Create("\"\" = 1\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, string.Empty);
        ExpectToken(ref reader, TomlTokenType.Integer);
    }

    /// <summary>
    /// Verifies that whitespace surrounding the dots of a dotted key is ignored, producing the same nested tables as a
    /// tightly spelled path.
    /// </summary>
    [TestMethod]
    public void Read_WhenDottedKeyHasWhitespaceAroundDots_ShouldSurfaceNestedTables()
    {
        Utf8TomlReader reader = Create("a . b . c = 1\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "a");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "b");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "c");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectEndTable(ref reader);
    }

    /// <summary>
    /// Verifies that a dotted key mixing bare and quoted segments resolves each segment to its decoded text.
    /// </summary>
    [TestMethod]
    public void Read_WhenDottedKeyMixesBareAndQuotedSegments_ShouldDecodeEachSegment()
    {
        Utf8TomlReader reader = Create("site.\"sub key\".value = 1\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "site");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "sub key");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "value");
        ExpectToken(ref reader, TomlTokenType.Integer);
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectEndTable(ref reader);
    }

    /// <summary>
    /// Verifies that a key with no value and a bare key followed by stray content are rejected with
    /// <see cref="TomlFormatException" />.
    /// </summary>
    /// <param name="source">The malformed key line under test.</param>
    [TestMethod]
    [DataRow("= 1\n", DisplayName = "missing key")]
    [DataRow("a b = 1\n", DisplayName = "space inside bare key")]
    [DataRow("a. = 1\n", DisplayName = "trailing dot")]
    [DataRow(".a = 1\n", DisplayName = "leading dot")]
    [DataRow("a..b = 1\n", DisplayName = "empty dotted segment")]
    [TestCategory("Regression")]
    public void Read_WhenKeyMalformed_ShouldThrowTomlFormatException(string source)
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create(source);
        });
    }
}
