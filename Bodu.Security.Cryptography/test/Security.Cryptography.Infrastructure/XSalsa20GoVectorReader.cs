// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20GoVectorReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the XSalsa20 test vectors transcribed in the Go <c>golang.org/x/crypto/salsa20</c> test source
/// (<c>xSalsa20TestData</c> array). Each record holds four byte literals in the order plaintext, nonce, key, and
/// expected ciphertext, expressed in one of three Go forms: an ASCII string (<c>[]byte("…")</c>), a zero buffer
/// (<c>make([]byte, N)</c>), or an explicit hex list (<c>[]byte{0x…}</c>).
/// </summary>
public static partial class XSalsa20GoVectorReader
{
    /// <summary>
    /// Reads the XSalsa20 encryption vectors from <paramref name="stream" />.
    /// </summary>
    /// <param name="stream">A readable stream over the Go Salsa20 test source.</param>
    /// <returns>The XSalsa20 vectors, in source order.</returns>
    public static IEnumerable<StreamCipherKnownAnswer> Read(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false);
        string text = reader.ReadToEnd();

        int start = text.IndexOf("xSalsa20TestData", StringComparison.Ordinal);
        if (start < 0)
            yield break;

        int end = text.IndexOf("func TestXSalsa20", start, StringComparison.Ordinal);
        if (end < 0)
            end = text.Length;

        string block = text[start..end];

        var fields = new List<byte[]>();
        foreach (Match field in ByteLiteralPattern().Matches(block))
            fields.Add(Decode(field));

        // Each record is four consecutive fields: plaintext, nonce, key, ciphertext.
        int number = 0;
        for (int i = 0; i + 3 < fields.Count; i += 4)
        {
            number++;
            yield return new StreamCipherKnownAnswer
            {
                Name = $"golang.org/x/crypto XSalsa20 #{number}",
                Provenance = KatProvenance.ReferenceImplementation("golang.org/x/crypto/salsa20 (NaCl xsalsa20 vectors)"),
                Plaintext = fields[i],
                Nonce = fields[i + 1],
                Key = fields[i + 2],
                Ciphertext = fields[i + 3],
                IsKeystream = false,
            };
        }
    }

    /// <summary>Decodes a single matched Go byte literal into its bytes.</summary>
    /// <param name="match">A match produced by <see cref="ByteLiteralPattern" />.</param>
    /// <returns>The decoded bytes.</returns>
    private static byte[] Decode(Match match)
    {
        if (match.Groups[1].Success)
            return Encoding.ASCII.GetBytes(match.Groups[1].Value);

        if (match.Groups[2].Success)
            return new byte[int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)];

        var bytes = new List<byte>();
        foreach (Match hex in HexBytePattern().Matches(match.Groups[3].Value))
            bytes.Add(byte.Parse(hex.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture));

        return [.. bytes];
    }

    [GeneratedRegex(@"\[\]byte\(""([^""]*)""\)|make\(\[\]byte,\s*(\d+)\)|\[\]byte\{([^}]*)\}")]
    private static partial Regex ByteLiteralPattern();

    [GeneratedRegex(@"0x([0-9A-Fa-f]{2})")]
    private static partial Regex HexBytePattern();
}
