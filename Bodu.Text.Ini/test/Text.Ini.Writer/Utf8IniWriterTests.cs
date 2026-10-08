// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

using Bodu.Text.Ini.Reader;
using Bodu.Text.Ini.Writer;

namespace Bodu.Text.Ini.Writer;

/// <summary>
/// Contains tests for <see cref="Utf8IniWriter" />, verifying the emitted bytes and round-trip fidelity against
/// <see cref="Utf8IniReader" />.
/// </summary>
[TestClass]
public partial class Utf8IniWriterTests
{
    /// <summary>The characters that matter to the INI dialect, which the short-name sweeps combine.</summary>
    private const string SweepAlphabet = "a \t=[];#\r\n\uFEFF";

    /// <summary>The longest name the short-name sweeps try.</summary>
    private const int SweepMaxLength = 4;

    /// <summary>
    /// Enumerates every name of one to <see cref="SweepMaxLength" /> characters drawn from <see cref="SweepAlphabet" />.
    /// </summary>
    /// <returns>The names, shortest first.</returns>
    private static IEnumerable<string> ShortNames()
    {
        int combinations = 1;
        for (int length = 1; length <= SweepMaxLength; length++)
        {
            combinations *= SweepAlphabet.Length;
            for (int combination = 0; combination < combinations; combination++)
            {
                var chars = new char[length];
                int rest = combination;
                for (int i = 0; i < length; i++)
                {
                    chars[i] = SweepAlphabet[rest % SweepAlphabet.Length];
                    rest /= SweepAlphabet.Length;
                }

                yield return new string(chars);
            }
        }
    }

    /// <summary>
    /// Reads INI bytes back as a transcript of <c>Kind:Text</c> tokens separated by <c>|</c>.
    /// </summary>
    /// <param name="utf8Ini">The INI bytes.</param>
    /// <returns>The transcript, or a description of the <see cref="IniFormatException" /> the bytes raise.</returns>
    private static string ReadBack(ReadOnlySpan<byte> utf8Ini)
    {
        var tokens = new List<string>();
        try
        {
            var reader = new Utf8IniReader(utf8Ini);
            while (reader.Read())
                tokens.Add($"{reader.TokenType}:{reader.GetString()}");
        }
        catch (IniFormatException ex)
        {
            tokens.Add($"throws IniFormatException: {ex.Message}");
        }

        return string.Join('|', tokens);
    }

    /// <summary>
    /// Writes text with its line breaks, tabs and U+FEFF escaped, for a failure message.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The escaped text, quoted.</returns>
    private static string Escape(string text) =>
        "\"" + text
            .Replace("\r", @"\r", StringComparison.Ordinal)
            .Replace("\n", @"\n", StringComparison.Ordinal)
            .Replace("\t", @"\t", StringComparison.Ordinal)
            .Replace("\uFEFF", @"\uFEFF", StringComparison.Ordinal) + "\"";

    /// <summary>
    /// Verifies that a global key, a section header, and section entries are emitted in order.
    /// </summary>
    [TestMethod]
    public void Write_WhenGlobalKeyAndSection_ShouldEmitExpectedText()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer);

        writer.WritePropertyName("key0");
        writer.WriteString("a");
        writer.WriteSectionHeader("db");
        writer.WritePropertyName("host");
        writer.WriteString("localhost");
        writer.Flush();

        Assert.AreEqual("key0=a\n[db]\nhost=localhost\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that a document written by <see cref="Utf8IniWriter" /> reads back to the same section/key/value tokens.
    /// </summary>
    [TestMethod]
    public void Write_WhenRoundTripped_ShouldPreserveTokens()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer);

        writer.WriteSectionHeader("server");
        writer.WritePropertyName("host");
        writer.WriteString("example.com");
        writer.WritePropertyName("port");
        writer.WriteString("8080");
        writer.Flush();

        var reader = new Utf8IniReader(buffer.WrittenSpan);
        var tokens = new List<string>();
        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                IniTokenType.SectionHeader => $"Section:{reader.GetString()}",
                IniTokenType.PropertyName => $"Name:{reader.GetString()}",
                IniTokenType.String => $"String:{reader.GetString()}",
                _ => reader.TokenType.ToString(),
            });
        }

        CollectionAssert.AreEqual(
            new List<string> { "Section:server", "Name:host", "String:example.com", "Name:port", "String:8080" },
            tokens);
    }
}
