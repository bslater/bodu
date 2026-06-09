// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderVersionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Tests for the TOML specification-version boundary enforced by <see cref="TomlReaderOptions.SpecVersion" />: the four
/// grammar features TOML v1.1.0 added (the <c>\e</c> and <c>\xHH</c> escapes, time values without seconds, and
/// multi-line and trailing-comma inline tables) must be rejected under the default strict v1.0.0 profile and accepted
/// under the v1.1.0 profile.
/// </summary>
[TestClass]
public sealed class TomlReaderVersionTests
{
    private static readonly TomlReaderOptions s_v11 = new() { SpecVersion = TomlSpecVersion.V1_1 };

    /// <summary>
    /// Verifies that <see cref="TomlReaderOptions" /> defaults to the strict TOML v1.0.0 profile.
    /// </summary>
    [TestMethod]
    public void SpecVersion_WhenOptionsAreDefaulted_ShouldBeV10() =>
        Assert.AreEqual(TomlSpecVersion.V1_0, new TomlReaderOptions().SpecVersion);

    /// <summary>
    /// Verifies that the parameterless parse path enforces strict TOML v1.0.0 by rejecting a v1.1-only construct.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNoOptions_ShouldDefaultToStrictV10() =>
        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("x = 07:32"));

    /// <summary>
    /// Verifies that each construct introduced by TOML v1.1.0 is rejected under the default strict v1.0.0 profile.
    /// </summary>
    /// <param name="name">The scenario name, used as the failure message.</param>
    /// <param name="source">The TOML source exercising a v1.1-only construct.</param>
    [TestMethod]
    [DataRow("escape-e", "s = \"\\e\"")]
    [DataRow("escape-x", "s = \"\\x41\"")]
    [DataRow("offset-datetime-no-seconds", "x = 1979-05-27T07:32Z")]
    [DataRow("local-datetime-no-seconds", "x = 1979-05-27T07:32")]
    [DataRow("local-time-no-seconds", "x = 07:32")]
    [DataRow("inline-table-multiline", "x = {\n  a = 1\n}")]
    [DataRow("inline-table-trailing-comma", "x = { a = 1, }")]
    public void Parse_WhenV11OnlySyntax_ForStrictV10_ShouldThrowTomlFormatException(string name, string source) =>
        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse(source), name);

    /// <summary>
    /// Verifies that each construct introduced by TOML v1.1.0 parses under the v1.1.0 profile.
    /// </summary>
    /// <param name="name">The scenario name, used as the failure message.</param>
    /// <param name="source">The TOML source exercising a v1.1-only construct.</param>
    [TestMethod]
    [DataRow("escape-e", "s = \"\\e\"")]
    [DataRow("escape-x", "s = \"\\x41\"")]
    [DataRow("offset-datetime-no-seconds", "x = 1979-05-27T07:32Z")]
    [DataRow("local-datetime-no-seconds", "x = 1979-05-27T07:32")]
    [DataRow("local-time-no-seconds", "x = 07:32")]
    [DataRow("inline-table-multiline", "x = {\n  a = 1\n}")]
    [DataRow("inline-table-trailing-comma", "x = { a = 1, }")]
    public void Parse_WhenV11OnlySyntax_ForV11Profile_ShouldParse(string name, string source) =>
        Assert.IsNotNull(Toml.Parse(source, s_v11), name);

    /// <summary>
    /// Verifies that the <c>\e</c> escape decodes to U+001B under the v1.1.0 profile.
    /// </summary>
    [TestMethod]
    public void Parse_WhenEscapeE_ForV11Profile_ShouldDecodeToEscapeCharacter()
    {
        TomlTable doc = Toml.Parse("s = \"\\e\"", s_v11);
        Assert.AreEqual("\u001b", ((TomlString)doc["s"]).Value);
    }

    /// <summary>
    /// Verifies that the <c>\xHH</c> escape decodes to the corresponding code point under the v1.1.0 profile.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHexEscape_ForV11Profile_ShouldDecodeByte()
    {
        TomlTable doc = Toml.Parse("s = \"\\x41\"", s_v11);
        Assert.AreEqual("A", ((TomlString)doc["s"]).Value);
    }

    /// <summary>
    /// Verifies that an offset date-time without seconds defaults the seconds to zero under the v1.1.0 profile.
    /// </summary>
    [TestMethod]
    public void Parse_WhenOffsetDateTimeWithoutSeconds_ForV11Profile_ShouldDefaultSecondsToZero()
    {
        TomlTable doc = Toml.Parse("x = 1979-05-27T07:32Z", s_v11);
        Assert.AreEqual(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero), ((TomlOffsetDateTime)doc["x"]).Value);
    }

    /// <summary>
    /// Verifies that a local time without seconds defaults the seconds to zero under the v1.1.0 profile.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLocalTimeWithoutSeconds_ForV11Profile_ShouldDefaultSecondsToZero()
    {
        TomlTable doc = Toml.Parse("x = 07:32", s_v11);
        Assert.AreEqual(new TimeOnly(7, 32, 0), ((TomlLocalTime)doc["x"]).Value);
    }

    /// <summary>
    /// Verifies that a multi-line, trailing-comma inline table reads its keys under the v1.1.0 profile.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMultilineInlineTable_ForV11Profile_ShouldReadKeys()
    {
        TomlTable doc = Toml.Parse("x = {\n  a = 1,\n  b = 2,\n}", s_v11);
        var x = (TomlTable)doc["x"];
        Assert.AreEqual(1, ((TomlInteger)x["a"]).Value);
        Assert.AreEqual(2, ((TomlInteger)x["b"]).Value);
    }

    /// <summary>
    /// Verifies that constructing a reader with a <see langword="null" /> options instance throws.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOptionsAreNull_ShouldThrowArgumentNullException() =>
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new TomlReader(null!));

    /// <summary>
    /// Verifies that a reader constructed with explicit v1.1.0 options accepts a v1.1-only construct.
    /// </summary>
    [TestMethod]
    public void Read_WhenReaderConfiguredForV11_ShouldAcceptV11Syntax()
    {
        var reader = new TomlReader(s_v11);
        TomlTable doc = reader.Read("x = 07:32");
        Assert.AreEqual(new TimeOnly(7, 32, 0), ((TomlLocalTime)doc["x"]).Value);
    }

    /// <summary>
    /// Verifies that the <see cref="TomlReaderOptions.SpecVersion" /> option flows through the stream parse overload, so
    /// a v1.1-only construct is accepted when the v1.1.0 profile is supplied.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStreamWithV11Options_ShouldApplyV11Profile()
    {
        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes("x = 07:32"));

        var doc = Toml.Parse(stream, s_v11);

        Assert.AreEqual(new TimeOnly(7, 32, 0), ((TomlLocalTime)doc["x"]).Value);
    }

    /// <summary>
    /// Verifies that the stream parse overload still defaults to the strict v1.0.0 profile, rejecting a v1.1-only
    /// construct when no options are supplied.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStreamWithoutOptions_ShouldDefaultToStrictV10()
    {
        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes("x = 07:32"));

        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse(stream));
    }

    /// <summary>
    /// Verifies that the <see cref="TomlReaderOptions.SpecVersion" /> option flows through the asynchronous stream parse
    /// overload.
    /// </summary>
    [TestMethod]
    public async Task ParseAsync_WhenStreamWithV11Options_ShouldApplyV11Profile()
    {
        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes("x = 07:32"));

        var doc = await Toml.ParseAsync(stream, s_v11);

        Assert.AreEqual(new TimeOnly(7, 32, 0), ((TomlLocalTime)doc["x"]).Value);
    }

    /// <summary>
    /// Verifies that the <see cref="TomlReaderOptions.SpecVersion" /> option flows through the try-parse overload — a
    /// v1.1-only construct succeeds under the v1.1.0 profile but fails under the strict v1.0.0 default.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenV11Options_ShouldApplyV11Profile()
    {
        Assert.IsTrue(Toml.TryParse("x = 07:32", s_v11, out var parsed));
        Assert.AreEqual(new TimeOnly(7, 32, 0), ((TomlLocalTime)parsed["x"]).Value);

        Assert.IsFalse(Toml.TryParse("x = 07:32", out _));
    }
}
