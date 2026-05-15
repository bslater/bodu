// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="Base16" />. Partial files split coverage by member or subject contract per the
/// repository test convention (see <c>CLAUDE.md</c>).
/// </summary>
[TestClass]
public sealed partial class Base16Tests
{
    /// <summary>
    /// A canonical four-byte input used as the reference vector across encode/decode round-trip tests.
    /// </summary>
    private static readonly byte[] CanonicalBytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

    /// <summary>
    /// The lower case hexadecimal representation of <see cref="CanonicalBytes" />.
    /// </summary>
    private const string CanonicalHexLower = "deadbeef";

    /// <summary>
    /// The upper case hexadecimal representation of <see cref="CanonicalBytes" />.
    /// </summary>
    private const string CanonicalHexUpper = "DEADBEEF";

    /// <summary>
    /// Returns a curated byte pattern used in round-trip and regression tests.
    /// </summary>
    /// <param name="key">The pattern key.</param>
    /// <returns>The byte pattern.</returns>
    private static byte[] GetPatternBytes(string key) => key switch
    {
        "zero-bytes" => new byte[32],
        "max-bytes" => Enumerable.Repeat((byte)0xFF, 32).ToArray(),
        "ascending" => Enumerable.Range(0, 32).Select(i => (byte)i).ToArray(),
        "descending" => Enumerable.Range(0, 32).Select(i => (byte)(255 - i)).ToArray(),
        "alternating-aa-55" => Enumerable.Range(0, 32).Select(i => i % 2 == 0 ? (byte)0xAA : (byte)0x55).ToArray(),
        "alternating-0f-f0" => Enumerable.Range(0, 32).Select(i => i % 2 == 0 ? (byte)0x0F : (byte)0xF0).ToArray(),
        "ascii-printable" => Enumerable.Range(32, 32).Select(i => (byte)i).ToArray(),
        "high-bytes" => Enumerable.Range(128, 32).Select(i => (byte)i).ToArray(),
        _ => throw new ArgumentException($"Unknown pattern key: {key}", nameof(key)),
    };
}
