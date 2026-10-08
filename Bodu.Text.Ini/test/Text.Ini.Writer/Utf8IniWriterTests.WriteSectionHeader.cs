// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniWriterTests.WriteSectionHeader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Ini.Writer;

/// <summary>
/// Contains the member tests for <see cref="Utf8IniWriter.WriteSectionHeader(string)" />.
/// </summary>
public partial class Utf8IniWriterTests
{
    /// <summary>
    /// Verifies that a section name the reader would read back as something else throws
    /// <see cref="ArgumentException" /> naming the name: an empty name, one with surrounding whitespace, one containing a
    /// line break, and one containing a <c>]</c> that a comment marker follows, where the reader would end the name.
    /// </summary>
    /// <param name="name">The section name to write.</param>
    [TestMethod]
    [DataRow("", DisplayName = "empty")]
    [DataRow(" s", DisplayName = "leading space")]
    [DataRow("s ", DisplayName = "trailing space")]
    [DataRow("\ts", DisplayName = "leading tab")]
    [DataRow("s\t", DisplayName = "trailing tab")]
    [DataRow("a\nb", DisplayName = "LF")]
    [DataRow("a\rb", DisplayName = "CR")]
    [DataRow("a];b", DisplayName = "bracket then semicolon")]
    [DataRow("a] ;b", DisplayName = "bracket, space, semicolon")]
    [DataRow("a]#b", DisplayName = "bracket then hash")]
    [DataRow("a]\t#b", DisplayName = "bracket, tab, hash")]
    public void WriteSectionHeader_WhenNameWouldReadBackDifferently_ShouldThrowArgumentException(string name)
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var writer = new Utf8IniWriter(new ArrayBufferWriter<byte>());
            writer.WriteSectionHeader(name);
        });

        Assert.AreEqual("name", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a section name holding <c>]</c>, <c>[</c>, comment markers or <c>=</c> where the reader reads them
    /// as part of the name is written and read back unchanged.
    /// </summary>
    /// <param name="name">The section name to write.</param>
    [TestMethod]
    [DataRow("foo]bar", DisplayName = "go-ini #46")]
    [DataRow("12345]", DisplayName = "iniparser PR #159, bracket at the end")]
    [DataRow("123]45", DisplayName = "iniparser PR #159, bracket inside")]
    [DataRow("This One Has A ] In It", DisplayName = "configparser bpo-38741")]
    [DataRow("a] b", DisplayName = "bracket, space, text")]
    [DataRow("]", DisplayName = "only a bracket")]
    [DataRow("[x", DisplayName = "opening bracket")]
    [DataRow("a;b", DisplayName = "semicolon")]
    [DataRow("#b", DisplayName = "leading hash")]
    [DataRow("x=y", DisplayName = "equals sign")]
    public void WriteSectionHeader_WhenNameIsAccepted_ShouldReadBackUnchanged(string name)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer);

        writer.WriteSectionHeader(name);
        writer.Flush();

        Assert.AreEqual($"SectionHeader:{name}", ReadBack(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that every section name of up to four characters drawn from the characters that matter to the dialect
    /// is either refused with <see cref="ArgumentException" /> or written so that it reads back unchanged.
    /// </summary>
    [TestMethod]
    public void WriteSectionHeader_WhenNameIsAnyShortCombination_ShouldRefuseItOrReadItBackUnchanged()
    {
        var failures = new List<string>();
        foreach (string name in ShortNames())
        {
            var buffer = new ArrayBufferWriter<byte>();
            try
            {
                var writer = new Utf8IniWriter(buffer);
                writer.WriteSectionHeader(name);
                writer.Flush();
            }
            catch (ArgumentException)
            {
                // Refused: the writer does not claim to write this name.
                continue;
            }

            string readBack = ReadBack(buffer.WrittenSpan);
            if (!string.Equals(readBack, $"SectionHeader:{name}", StringComparison.Ordinal))
                failures.Add($"{Escape(name)} read back as {Escape(readBack)}");
        }

        Assert.AreEqual(0, failures.Count, $"{failures.Count} names:{Environment.NewLine}{string.Join(Environment.NewLine, failures.Take(20))}");
    }
}
