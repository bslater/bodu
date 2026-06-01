// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="EncodingExtensions" />. Partial files split coverage by member or subject
/// contract per the repository test convention (see <c>CLAUDE.md</c>).
/// </summary>
[TestClass]
public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// A canonical UTF-16 character span used as the reference input across encode/decode round-trip tests.
    /// </summary>
    private const string SampleText = "hello, world";

    /// <summary>
    /// A text containing multi-byte UTF-8 sequences (Latin-1, Cyrillic, CJK, emoji) used to exercise non-ASCII
    /// paths and assert that wide-byte counts are reported correctly.
    /// </summary>
    private const string MultiByteText = "héllo — Привет — 你好 — \U0001F600";

    /// <summary>
    /// Returns a curated character pattern used in encode and round-trip tests.
    /// </summary>
    /// <param name="key">The pattern key.</param>
    /// <returns>The character span as a string.</returns>
    private static string GetPatternText(string key) => key switch
    {
        "empty" => string.Empty,
        "ascii" => SampleText,
        "multi-byte" => MultiByteText,
        "single-emoji" => "\U0001F4A9",
        "ascii-extended" => new string(System.Linq.Enumerable.Range(0, 128).Select(i => (char)i).ToArray()),
        _ => throw new ArgumentException($"Unknown pattern key: {key}", nameof(key)),
    };

    /// <summary>
    /// Returns the canonical encodings exercised across overload tests so that single-byte, two-byte, four-byte,
    /// and big-endian variants are all covered.
    /// </summary>
    /// <returns>The encodings.</returns>
    private static System.Collections.Generic.IEnumerable<object[]> CanonicalEncodings()
    {
        yield return new object[] { System.Text.Encoding.UTF8 };
        yield return new object[] { System.Text.Encoding.Unicode };
        yield return new object[] { System.Text.Encoding.BigEndianUnicode };
        yield return new object[] { System.Text.Encoding.UTF32 };
        yield return new object[] { System.Text.Encoding.ASCII };
    }
}
