// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DecodeAgreement.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Supplies the inputs and the oracle for the sweeps that hold each encoding's validation members (<c>IsValid</c>,
/// <c>TryGetDecodedLength</c>, and <c>GetDecodedLength</c>) to its <c>Decode</c> member, which is the reference: a
/// validation member must accept exactly the input <c>Decode</c> accepts with the same variant and styles, and report
/// the length <c>Decode</c> returns.
/// </summary>
internal static class DecodeAgreement
{
    /// <summary>The largest number of disagreements quoted in a failure message.</summary>
    private const int MaxQuoted = 20;

    /// <summary>The number of characters of a long input quoted in a failure message.</summary>
    private const int MaxQuotedLength = 48;

    /// <summary>Gets every combination of the four <see cref="BaseFormatStyles" /> flags.</summary>
    internal static BaseFormatStyles[] AllStyles { get; } = [.. Enumerable.Range(0, 16).Select(i => (BaseFormatStyles)i)];

    /// <summary>
    /// Fails the calling test when <paramref name="disagreements" /> is not empty, quoting the first of them.
    /// </summary>
    /// <param name="disagreements">The disagreements the sweep recorded.</param>
    internal static void AssertNone(IReadOnlyCollection<string> disagreements) =>
        Assert.AreEqual(
            0,
            disagreements.Count,
            $"{disagreements.Count} disagreement(s) with Decode, for example:{Environment.NewLine}"
                + string.Join(Environment.NewLine, disagreements.Take(MaxQuoted)));

    /// <summary>
    /// Returns <paramref name="value" /> with its control characters written as C# escapes, and a long value cut to its
    /// first characters and its length, so that a quoted input stays on one line of a failure message.
    /// </summary>
    /// <param name="value">The input to quote.</param>
    /// <returns>The quoted input.</returns>
    internal static string Quote(string value)
    {
        var escaped = value.Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);

        return escaped.Length <= MaxQuotedLength
            ? "\"" + escaped + "\""
            : $"\"{escaped[..MaxQuotedLength]}...\" ({value.Length} characters)";
    }

    /// <summary>
    /// Returns <paramref name="encoded" /> together with variants of it that probe a decoder's structural rules:
    /// padding added, removed, or in excess; the final symbol replaced by each of <paramref name="symbols" /> (which
    /// produces non-canonical tails and, for the block encodings, group overflow); the last character dropped;
    /// whitespace, prefix, and delimiter decoration; and a character outside every alphabet.
    /// </summary>
    /// <param name="encoded">A well-formed encoding.</param>
    /// <param name="symbols">The characters substituted for the final symbol.</param>
    /// <returns>The input and its variants.</returns>
    internal static IEnumerable<string> Mutations(string encoded, string symbols)
    {
        yield return encoded;
        yield return encoded + "=";
        yield return encoded + "==";
        yield return encoded + "===";
        yield return encoded + "========";
        yield return " " + encoded;
        yield return encoded + "\r\n";
        yield return "0x" + encoded;
        yield return "<~" + encoded + "~>";
        yield return encoded + "é";

        if (encoded.Length == 0)
            yield break;

        yield return encoded[..^1];

        if (encoded.Length > 2)
            yield return encoded[..2] + " " + encoded[2..];

        string data = encoded.TrimEnd('=');
        string padding = encoded[data.Length..];
        if (padding.Length > 0)
        {
            yield return data;
            yield return data + padding[1..];
        }

        if (data.Length == 0)
            yield break;

        foreach (var symbol in symbols)
        {
            yield return data[..^1] + symbol + padding;
            yield return data[..^1] + symbol;
        }
    }

    /// <summary>
    /// Returns payloads of every length from zero to <paramref name="maxLength" />: for each length a seeded random
    /// payload, an all-zero payload, and an all-<c>0xFF</c> payload.
    /// </summary>
    /// <param name="maxLength">The longest payload length.</param>
    /// <returns>The payloads.</returns>
    internal static IEnumerable<byte[]> Payloads(int maxLength)
    {
        var random = new Random(0x5EED);

        for (var length = 0; length <= maxLength; length++)
        {
            var randomPayload = new byte[length];
            random.NextBytes(randomPayload);
            yield return randomPayload;

            yield return new byte[length];

            var ones = new byte[length];
            Array.Fill(ones, (byte)0xFF);
            yield return ones;
        }
    }

    /// <summary>
    /// Runs <paramref name="decode" /> and reports whether it succeeded, treating <see cref="FormatException" /> as
    /// the decoder's rejection of the input.
    /// </summary>
    /// <param name="decode">The decode call under test.</param>
    /// <param name="length">
    /// When this method returns <see langword="true" />, the length of the decoded bytes; otherwise <c>-1</c>.
    /// </param>
    /// <returns><see langword="true" /> when the decode returned; <see langword="false" /> when it rejected the input.</returns>
    internal static bool TryDecode(Func<byte[]> decode, out int length)
    {
        try
        {
            length = decode().Length;
            return true;
        }
        catch (FormatException)
        {
            length = -1;
            return false;
        }
    }
}
