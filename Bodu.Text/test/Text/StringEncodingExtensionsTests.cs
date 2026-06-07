// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

/// <summary>
/// Behavioural tests for <see cref="StringEncodingExtensions" />. Partial files split coverage by member per
/// the repository test convention (see <c>CLAUDE.md</c>).
/// </summary>
[TestClass]
public sealed partial class StringEncodingExtensionsTests
{
    /// <summary>
    /// A canonical UTF-16 string used as the reference input across encode tests.
    /// </summary>
    private const string SampleText = "hello, world";

    /// <summary>
    /// A string containing multi-byte UTF-8 sequences used to exercise non-ASCII encoding paths.
    /// </summary>
    private const string MultiByteText = "héllo — Привет — 你好 — \U0001F600";

    /// <summary>
    /// Returns the canonical encodings exercised across overload tests so single-byte, two-byte, four-byte, and
    /// big-endian variants are all covered.
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
