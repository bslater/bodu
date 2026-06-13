// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.Keys.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that a key matching the bare-key grammar is emitted without quoting, while a key containing illegal
    /// bare-key characters is basic-quoted with escaping.
    /// </summary>
    /// <param name="key">The property key to write.</param>
    /// <param name="expectedKey">The expected emitted key text, quoted where required.</param>
    [TestMethod]
    [DataRow("bareKey", "bareKey", DisplayName = "bare letters")]
    [DataRow("a-b_c", "a-b_c", DisplayName = "bare with hyphen and underscore")]
    [DataRow("1234", "1234", DisplayName = "bare digits")]
    [DataRow("needs quoting", "\"needs quoting\"", DisplayName = "space forces quoting")]
    [DataRow("a.b", "\"a.b\"", DisplayName = "dot forces quoting")]
    [DataRow("café", "\"café\"", DisplayName = "unicode forces quoting")]
    [DataRow("", "\"\"", DisplayName = "empty key is quoted")]
    [TestCategory("Regression")]
    public void Write_WhenKey_ShouldQuoteOnlyWhenRequired(string key, string expectedKey)
    {
        var actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName(key);
            writer.WriteInteger(1);
            writer.WriteEndTable();
        });

        Assert.AreEqual($"{expectedKey} = 1\n", actual);
    }

    /// <summary>
    /// Verifies that a key requiring quoting escapes reserved characters within the emitted basic-quoted key.
    /// </summary>
    [TestMethod]
    public void Write_WhenKeyContainsReservedCharacter_ShouldEscapeWithinQuotes()
    {
        var actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("a\"b");
            writer.WriteInteger(1);
            writer.WriteEndTable();
        });

        Assert.AreEqual("\"a\\\"b\" = 1\n", actual);
    }

    /// <summary>
    /// Verifies that a sub-table header path quotes only the segments that require it, leaving bare segments unquoted,
    /// and emits a header for each intermediate table even when it carries no scalar members.
    /// </summary>
    [TestMethod]
    public void Write_WhenSubTableHeaderHasMixedSegments_ShouldQuotePerSegment()
    {
        var actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("server");
            writer.WriteStartTable();
            writer.WritePropertyName("data center");
            writer.WriteStartTable();
            writer.WritePropertyName("id");
            writer.WriteInteger(1);
            writer.WriteEndTable();
            writer.WriteEndTable();
            writer.WriteEndTable();
        });

        Assert.AreEqual("[server]\n\n[server.\"data center\"]\nid = 1\n", actual);
    }
}
