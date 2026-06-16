// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bech32.Encode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Encoding;

public static partial class Bech32
{
    /// <summary>
    /// Encodes 5-bit <paramref name="data" /> groups under <paramref name="hrp" /> into a Bech32 or Bech32m string with
    /// an appended six-symbol checksum.
    /// </summary>
    /// <param name="hrp">The human-readable part. Lower-cased to produce canonical output.</param>
    /// <param name="data">The data groups, each a 5-bit value in the range 0 to 31.</param>
    /// <param name="encoding">The scheme selecting the checksum constant.</param>
    /// <returns>The lower-case encoded string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="hrp" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="hrp" /> is empty or contains a character outside the US-ASCII range 33 to 126, or
    /// when any element of <paramref name="data" /> exceeds 31.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="encoding" /> is undefined.</exception>
    /// <remarks>
    /// This method does not impose the BIP-173 90-character maximum, so it can serve longer non-address schemes (for
    /// example Lightning BOLT11 invoices). The decoder enforces the 90-character limit by default.
    /// </remarks>
    public static string Encode(string hrp, ReadOnlySpan<byte> data, Bech32Encoding encoding = Bech32Encoding.Bech32)
    {
        ThrowHelper.ThrowIfNull(hrp);
        ValidateHrp(hrp);
        ValidateData(data);

        return EncodeCore(hrp, data, encoding);
    }

    /// <summary>
    /// Encodes 8-bit <paramref name="data" /> bytes under <paramref name="hrp" /> by first repacking them into 5-bit
    /// groups (with zero padding) and then encoding.
    /// </summary>
    /// <param name="hrp">The human-readable part. Lower-cased to produce canonical output.</param>
    /// <param name="data">The raw bytes to encode.</param>
    /// <param name="encoding">The scheme selecting the checksum constant.</param>
    /// <returns>The lower-case encoded string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="hrp" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="hrp" /> is empty or contains a character outside the US-ASCII range 33 to 126.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="encoding" /> is undefined.</exception>
    public static string EncodeFromBytes(string hrp, ReadOnlySpan<byte> data, Bech32Encoding encoding = Bech32Encoding.Bech32)
    {
        ThrowHelper.ThrowIfNull(hrp);
        ValidateHrp(hrp);

        // 8-bit input always converts cleanly into 5-bit groups, so ConvertBits cannot fail here.
        byte[]? groups = ConvertBits(data, 8, 5, pad: true);
        return EncodeCore(hrp, groups!, encoding);
    }

    /// <summary>
    /// Attempts to encode 5-bit <paramref name="data" /> groups under <paramref name="hrp" /> without throwing.
    /// </summary>
    /// <param name="hrp">The human-readable part.</param>
    /// <param name="data">The data groups, each a 5-bit value in the range 0 to 31.</param>
    /// <param name="encoding">The scheme selecting the checksum constant.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the encoded string; otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the parts are valid and encoding succeeds; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="encoding" /> is undefined.</exception>
    public static bool TryEncode(string hrp, ReadOnlySpan<byte> data, Bech32Encoding encoding, [NotNullWhen(true)] out string? result)
    {
        result = null;

        if (hrp is null || !IsValidHrp(hrp) || !IsValidData(data))
            return false;

        result = EncodeCore(hrp, data, encoding);
        return true;
    }

    /// <summary>
    /// Builds the encoded string for a validated <paramref name="hrp" /> and <paramref name="data" />.
    /// </summary>
    /// <param name="hrp">The validated human-readable part.</param>
    /// <param name="data">The validated 5-bit data groups.</param>
    /// <param name="encoding">The scheme selecting the checksum constant.</param>
    /// <returns>The lower-case encoded string.</returns>
    private static string EncodeCore(string hrp, ReadOnlySpan<byte> data, Bech32Encoding encoding)
    {
        string lowerHrp = hrp.ToLowerInvariant();

        Span<byte> checksum = stackalloc byte[ChecksumSymbols];
        CreateChecksum(lowerHrp, data, encoding, checksum);

        char[] output = new char[lowerHrp.Length + 1 + data.Length + ChecksumSymbols];
        int position = 0;

        lowerHrp.AsSpan().CopyTo(output);
        position += lowerHrp.Length;
        output[position++] = Separator;

        foreach (byte value in data)
            output[position++] = Charset[value];

        foreach (byte value in checksum)
            output[position++] = Charset[value];

        return new string(output);
    }

    /// <summary>
    /// Validates a human-readable part, throwing when it is empty or contains an out-of-range character.
    /// </summary>
    /// <param name="hrp">The human-readable part.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="hrp" /> is invalid.</exception>
    private static void ValidateHrp(string hrp)
    {
        if (!IsValidHrp(hrp))
            throw new ArgumentException(EncodingResourceStrings.Arg_Invalid_Bech32Hrp, nameof(hrp));
    }

    /// <summary>
    /// Validates 5-bit data groups, throwing when any value exceeds 31.
    /// </summary>
    /// <param name="data">The data groups.</param>
    /// <exception cref="ArgumentException">Thrown when an element of <paramref name="data" /> exceeds 31.</exception>
    private static void ValidateData(ReadOnlySpan<byte> data)
    {
        if (!IsValidData(data))
            throw new ArgumentException(EncodingResourceStrings.Arg_Invalid_Bech32DataValue, nameof(data));
    }

    /// <summary>
    /// Indicates whether <paramref name="hrp" /> is a non-empty sequence of US-ASCII characters in the range 33 to 126.
    /// </summary>
    /// <param name="hrp">The human-readable part.</param>
    /// <returns><see langword="true" /> when the part is valid.</returns>
    private static bool IsValidHrp(string hrp)
    {
        if (hrp.Length == 0)
            return false;

        foreach (char c in hrp)
        {
            if (c < MinHrpChar || c > MaxHrpChar)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Indicates whether every element of <paramref name="data" /> is a 5-bit value in the range 0 to 31.
    /// </summary>
    /// <param name="data">The data groups.</param>
    /// <returns><see langword="true" /> when every value is in range.</returns>
    private static bool IsValidData(ReadOnlySpan<byte> data)
    {
        foreach (byte value in data)
        {
            if (value > 31)
                return false;
        }

        return true;
    }
}
