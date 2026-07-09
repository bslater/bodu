// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20EcryptVerifiedReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the official ECRYPT Stream Cipher Project "verified" Salsa20 test-vector file. The file is organized into
/// key-size profiles (<c>Key size: 128 bits</c> / <c>256 bits</c>), each holding six numbered sets of
/// <c>Set N, vector# M</c> records. Every record declares a <c>key</c>, an <c>IV</c>, one or more <c>stream[a..b]</c>
/// keystream fragments, and a <c>xor-digest</c> — the XOR of every 64-byte block over the full generated keystream.
/// Multi-line hex values are folded back together.
/// </summary>
public static partial class Salsa20EcryptVerifiedReader
{
    /// <summary>
    /// Reads every Salsa20 vector from <paramref name="stream" />, across both key-size profiles.
    /// </summary>
    /// <param name="stream">A readable stream over the ECRYPT verified Salsa20 vector file.</param>
    /// <returns>The vectors, in source order.</returns>
    public static IEnumerable<Salsa20EcryptVector> Read(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false);

        int keyBits = 0;
        int set = 0;
        int? vector = null;
        var keyBuilder = new StringBuilder();
        string ivHex = string.Empty;
        var fragments = new List<(int Offset, int End, StringBuilder Hex)>();
        var digest = new StringBuilder();
        StringBuilder? current = null;

        Salsa20EcryptVector Build()
        {
            var frags = new List<(int, byte[])>(fragments.Count);
            int length = 0;
            foreach ((int offset, int end, StringBuilder hex) in fragments)
            {
                frags.Add((offset, Convert.FromHexString(hex.ToString())));
                length = Math.Max(length, end + 1);
            }

            return new Salsa20EcryptVector(
                keyBits,
                set,
                vector!.Value,
                Convert.FromHexString(keyBuilder.ToString()),
                Convert.FromHexString(ivHex),
                frags,
                Convert.FromHexString(digest.ToString()),
                length);
        }

        void Reset()
        {
            keyBuilder = new StringBuilder();
            ivHex = string.Empty;
            fragments = [];
            digest = new StringBuilder();
            current = null;
        }

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            Match keySize = KeySizePattern().Match(line);
            if (keySize.Success)
            {
                keyBits = int.Parse(keySize.Groups[1].Value, CultureInfo.InvariantCulture);
                continue;
            }

            Match setHeader = SetHeaderPattern().Match(line);
            if (setHeader.Success)
            {
                set = int.Parse(setHeader.Groups[1].Value, CultureInfo.InvariantCulture);
                continue;
            }

            Match record = RecordPattern().Match(line);
            if (record.Success)
            {
                if (vector is not null)
                    yield return Build();

                vector = int.Parse(record.Groups[1].Value, CultureInfo.InvariantCulture);
                Reset();
                continue;
            }

            if (vector is null)
                continue;

            Match key = KeyPattern().Match(line);
            if (key.Success)
            {
                keyBuilder.Append(key.Groups[1].Value);
                current = keyBuilder;
                continue;
            }

            Match iv = IvPattern().Match(line);
            if (iv.Success)
            {
                ivHex = iv.Groups[1].Value;
                current = null;
                continue;
            }

            Match fragment = FragmentPattern().Match(line);
            if (fragment.Success)
            {
                var builder = new StringBuilder(fragment.Groups[3].Value);
                fragments.Add((
                    int.Parse(fragment.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(fragment.Groups[2].Value, CultureInfo.InvariantCulture),
                    builder));
                current = builder;
                continue;
            }

            Match xor = XorDigestPattern().Match(line);
            if (xor.Success)
            {
                digest.Append(xor.Groups[1].Value);
                current = digest;
                continue;
            }

            Match continuation = ContinuationPattern().Match(line);
            if (continuation.Success && current is not null)
                current.Append(continuation.Groups[1].Value);
        }

        if (vector is not null)
            yield return Build();
    }

    [GeneratedRegex(@"Key size:\s*(\d+)\s*bits")]
    private static partial Regex KeySizePattern();

    [GeneratedRegex(@"Test vectors -- set (\d+)")]
    private static partial Regex SetHeaderPattern();

    [GeneratedRegex(@"vector#\s*(\d+):")]
    private static partial Regex RecordPattern();

    [GeneratedRegex(@"^\s*key\s*=\s*([0-9A-Fa-f]+)\s*$")]
    private static partial Regex KeyPattern();

    [GeneratedRegex(@"^\s*IV\s*=\s*([0-9A-Fa-f]+)\s*$")]
    private static partial Regex IvPattern();

    [GeneratedRegex(@"^\s*stream\[(\d+)\.\.(\d+)\]\s*=\s*([0-9A-Fa-f]+)\s*$")]
    private static partial Regex FragmentPattern();

    [GeneratedRegex(@"^\s*xor-digest\s*=\s*([0-9A-Fa-f]+)\s*$")]
    private static partial Regex XorDigestPattern();

    [GeneratedRegex(@"^\s+([0-9A-Fa-f]+)\s*$")]
    private static partial Regex ContinuationPattern();
}
