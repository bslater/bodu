// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20EcryptGoVectorReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the ECRYPT Salsa20 "Set 6" verified test vectors as transcribed in the Go <c>golang.org/x/crypto/salsa20</c>
/// test source (<c>testVectors</c> array). Each record carries a 256-bit key, a 64-bit IV, a keystream length, and the
/// 64-byte XOR digest — the running XOR of every 64-byte keystream block over the full length.
/// </summary>
public static partial class Salsa20EcryptGoVectorReader
{
    /// <summary>
    /// Reads the Set 6 XOR-digest vectors from <paramref name="stream" />.
    /// </summary>
    /// <param name="stream">A readable stream over the Go Salsa20 test source.</param>
    /// <returns>The Set 6 vectors, in source order.</returns>
    public static IEnumerable<Salsa20XorDigestVector> Read(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false);
        string text = reader.ReadToEnd();

        int start = text.IndexOf("testVectors", StringComparison.Ordinal);
        if (start < 0)
            yield break;

        int index = 0;
        foreach (Match record in RecordPattern().Matches(text, start))
        {
            index++;
            yield return new Salsa20XorDigestVector(
                index,
                Convert.FromHexString(record.Groups[1].Value),
                Convert.FromHexString(record.Groups[2].Value),
                int.Parse(record.Groups[3].Value, CultureInfo.InvariantCulture),
                Convert.FromHexString(record.Groups[4].Value));
        }
    }

    [GeneratedRegex(@"fromHex\(""([0-9A-Fa-f]+)""\),\s*fromHex\(""([0-9A-Fa-f]+)""\),\s*(\d+),\s*fromHex\(""([0-9A-Fa-f]+)""\)")]
    private static partial Regex RecordPattern();
}
