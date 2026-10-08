// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniWriterTests.WritePropertyName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Ini.Writer;

/// <summary>
/// Contains the member tests for <see cref="Utf8IniWriter.WritePropertyName(string)" />.
/// </summary>
public partial class Utf8IniWriterTests
{
    /// <summary>
    /// Verifies that a key the reader would read back as something else throws <see cref="ArgumentException" /> naming
    /// the key: an empty key, one with surrounding whitespace, one containing <c>=</c> or a line break, and one beginning
    /// with <c>[</c>, <c>;</c> or <c>#</c>.
    /// </summary>
    /// <param name="name">The key to write.</param>
    [TestMethod]
    [DataRow("", DisplayName = "empty")]
    [DataRow(" k", DisplayName = "leading space")]
    [DataRow("k ", DisplayName = "trailing space")]
    [DataRow("\tk", DisplayName = "leading tab")]
    [DataRow("k\t", DisplayName = "trailing tab")]
    [DataRow("a=b", DisplayName = "equals sign, configparser #65697")]
    [DataRow("=", DisplayName = "only an equals sign")]
    [DataRow("a\nb", DisplayName = "LF")]
    [DataRow("a\rb", DisplayName = "CR")]
    [DataRow("[this parses back as a section]", DisplayName = "leading bracket, configparser #65697")]
    [DataRow("[disturbing]", DisplayName = "leading bracket, npm-ini b80890bf43")]
    [DataRow(";k", DisplayName = "leading semicolon")]
    [DataRow("#k", DisplayName = "leading hash")]
    public void WritePropertyName_WhenKeyWouldReadBackDifferently_ShouldThrowArgumentException(string name)
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var writer = new Utf8IniWriter(new ArrayBufferWriter<byte>());
            writer.WritePropertyName(name);
        });

        Assert.AreEqual("name", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a key holding brackets, comment markers or whitespace inside it, which the reader reads literally,
    /// is written and read back unchanged.
    /// </summary>
    /// <param name="name">The key to write.</param>
    [TestMethod]
    [DataRow("a b", DisplayName = "inner space")]
    [DataRow("a\tb", DisplayName = "inner tab")]
    [DataRow("k[0]", DisplayName = "inner brackets")]
    [DataRow("a]", DisplayName = "closing bracket")]
    [DataRow("a;b", DisplayName = "inner semicolon")]
    [DataRow("a#b", DisplayName = "inner hash")]
    [DataRow("x:y", DisplayName = "colon")]
    [DataRow("café", DisplayName = "non-ASCII")]
    public void WritePropertyName_WhenKeyIsAccepted_ShouldReadBackUnchanged(string name)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer);

        writer.WritePropertyName(name);
        writer.WriteString("v");
        writer.Flush();

        Assert.AreEqual($"PropertyName:{name}|String:v", ReadBack(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that every key of up to four characters drawn from the characters that matter to the dialect is either
    /// refused with <see cref="ArgumentException" /> or written so that it reads back unchanged.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenKeyIsAnyShortCombination_ShouldRefuseItOrReadItBackUnchanged()
    {
        var failures = new List<string>();
        foreach (string name in ShortNames())
        {
            var buffer = new ArrayBufferWriter<byte>();
            try
            {
                var writer = new Utf8IniWriter(buffer);
                writer.WritePropertyName(name);
                writer.WriteString("v");
                writer.Flush();
            }
            catch (ArgumentException)
            {
                // Refused: the writer does not claim to write this key.
                continue;
            }

            string readBack = ReadBack(buffer.WrittenSpan);
            if (!string.Equals(readBack, $"PropertyName:{name}|String:v", StringComparison.Ordinal))
                failures.Add($"{Escape(name)} read back as {Escape(readBack)}");
        }

        Assert.AreEqual(0, failures.Count, $"{failures.Count} keys:{Environment.NewLine}{string.Join(Environment.NewLine, failures.Take(20))}");
    }
}
