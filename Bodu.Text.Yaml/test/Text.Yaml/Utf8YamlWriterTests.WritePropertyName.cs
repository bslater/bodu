// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriterTests.WritePropertyName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="Utf8YamlWriter.WritePropertyName" /> emits the expected output and enforces its contract.
/// </summary>
public partial class Utf8YamlWriterTests
{
    /// <summary>
    /// Verifies that writing a property name outside a mapping throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenNotInMapping_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8YamlWriter(buffer);
            writer.WriteStartSequence();
            writer.WritePropertyName("a");
        });
    }

    /// <summary>
    /// Verifies that a key whose written form is longer than the 1024 characters an implicit key may hold is written as
    /// an explicit key, with the value indicator on the next line.
    /// </summary>
    /// <param name="unit">The character the key repeats.</param>
    /// <param name="count">The number of characters in the key.</param>
    /// <param name="prefix">The text before the run, which may make the key need quotes.</param>
    /// <param name="quoted">Whether the key is written in double quotes.</param>
    [TestMethod]
    [DataRow("k", 1025, "", false, DisplayName = "a plain key of 1025 characters")]
    [DataRow("\uD83D\uDE00", 1025, "", false, DisplayName = "1025 characters outside the Basic Multilingual Plane")]
    [DataRow("k", 1022, "-", true, DisplayName = "a quoted key of 1025 characters with its quotes")]
    public void WritePropertyName_WhenKeyExceeds1024Characters_ShouldWriteExplicitKey(
        string unit,
        int count,
        string prefix,
        bool quoted)
    {
        string key = prefix + string.Concat(Enumerable.Repeat(unit, count));
        string written = quoted ? "\"" + key + "\"" : key;

        string yaml = Write(writer =>
        {
            writer.WriteStartMapping();
            writer.WritePropertyName(key);
            writer.WriteString("v");
            writer.WriteEndMapping();
        });

        Assert.AreEqual("? " + written + "\n: v\n", yaml);
    }

    /// <summary>
    /// Verifies that a key whose written form holds at most the 1024 characters an implicit key may hold is written as
    /// an implicit key.
    /// </summary>
    /// <param name="unit">The character the key repeats.</param>
    /// <param name="count">The number of characters in the key.</param>
    /// <param name="prefix">The text before the run, which may make the key need quotes.</param>
    /// <param name="quoted">Whether the key is written in double quotes.</param>
    [TestMethod]
    [DataRow("k", 1024, "", false, DisplayName = "a plain key of 1024 characters")]
    [DataRow("\uD83D\uDE00", 1024, "", false, DisplayName = "1024 characters outside the Basic Multilingual Plane")]
    [DataRow("k", 1021, "-", true, DisplayName = "a quoted key of 1024 characters with its quotes")]
    public void WritePropertyName_WhenKeyHasAtMost1024Characters_ShouldWriteImplicitKey(
        string unit,
        int count,
        string prefix,
        bool quoted)
    {
        string key = prefix + string.Concat(Enumerable.Repeat(unit, count));
        string written = quoted ? "\"" + key + "\"" : key;

        string yaml = Write(writer =>
        {
            writer.WriteStartMapping();
            writer.WritePropertyName(key);
            writer.WriteString("v");
            writer.WriteEndMapping();
        });

        Assert.AreEqual(written + ": v\n", yaml);
    }

    /// <summary>
    /// Verifies that an explicit key whose value is a collection is followed by a value indicator on its own line, or,
    /// for an empty collection, by the indicator and the collection's empty form.
    /// </summary>
    /// <param name="value">The kind of collection the key's value is.</param>
    /// <param name="expectedValue">The text expected after the key's line.</param>
    [TestMethod]
    [DataRow("mapping", ":\n  a: 1\n", DisplayName = "a mapping")]
    [DataRow("sequence", ":\n  - 1\n", DisplayName = "a sequence")]
    [DataRow("empty mapping", ": {}\n", DisplayName = "an empty mapping")]
    [DataRow("empty sequence", ": []\n", DisplayName = "an empty sequence")]
    public void WritePropertyName_WhenExplicitKeyHasCollectionValue_ShouldWriteIndicatorBeforeIt(
        string value,
        string expectedValue)
    {
        string key = new('k', 1025);

        string yaml = Write(writer =>
        {
            writer.WriteStartMapping();
            writer.WritePropertyName(key);
            if (value.EndsWith("mapping", StringComparison.Ordinal))
            {
                writer.WriteStartMapping();
                if (value == "mapping")
                {
                    writer.WritePropertyName("a");
                    writer.WriteInteger(1);
                }

                writer.WriteEndMapping();
            }
            else
            {
                writer.WriteStartSequence();
                if (value == "sequence")
                    writer.WriteInteger(1);

                writer.WriteEndSequence();
            }

            writer.WriteEndMapping();
        });

        Assert.AreEqual("? " + key + "\n" + expectedValue, yaml);
    }

    /// <summary>
    /// Verifies that the key <c>&lt;&lt;</c> is written quoted, so that a reader that expands merge keys reads it back
    /// as the ordinary key rather than as a merge key.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenKeyIsMergeKeyText_ShouldQuote()
    {
        string yaml = Write(writer =>
        {
            writer.WriteStartMapping();
            writer.WritePropertyName("<<");
            writer.WriteStartSequence();
            writer.WriteEndSequence();
            writer.WriteEndMapping();
        });

        Assert.AreEqual("\"<<\": []\n", yaml);
    }
}
