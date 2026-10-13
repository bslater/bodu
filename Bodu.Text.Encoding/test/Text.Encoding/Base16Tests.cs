// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// The lower case hexadecimal representation of <see cref="CanonicalBytes" />.
    /// </summary>
    private const string CanonicalHexLower = "deadbeef";

    /// <summary>
    /// The upper case hexadecimal representation of <see cref="CanonicalBytes" />.
    /// </summary>
    private const string CanonicalHexUpper = "DEADBEEF";
    /// <summary>
    /// A canonical four-byte input used as the reference vector across encode/decode round-trip tests.
    /// </summary>
    private static readonly byte[] CanonicalBytes = [0xDE, 0xAD, 0xBE, 0xEF];

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

    /// <summary>
    /// Returns the inputs the validation sweeps hold to <see cref="Base16.Decode(string, BaseFormatStyles)" />: plain,
    /// prefixed, and spaced encodings of every payload up to five bytes, each with the mutations of
    /// <see cref="DecodeAgreement.Mutations" />.
    /// </summary>
    /// <returns>The distinct inputs.</returns>
    private static HashSet<string> GetValidationSweepInputs()
    {
        const string symbols = "0123456789abcdefABCDEFgxX";
        var inputs = new HashSet<string>(StringComparer.Ordinal) { "0x", "0X", "0xdead", " 0xde ad", "0x0x00" };

        foreach (var payload in DecodeAgreement.Payloads(5))
        {
            foreach (BaseFormattingOptions options in new[] { BaseFormattingOptions.None, BaseFormattingOptions.IncludePrefix, BaseFormattingOptions.InsertSpacing })
                inputs.UnionWith(DecodeAgreement.Mutations(Base16.Encode(payload, options), symbols));
        }

        return inputs;
    }

}
