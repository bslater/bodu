// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bech32.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Encoding;

public static partial class Bech32
{
    /// <summary>
    /// Decodes a Bech32 or Bech32m string into its human-readable part and 5-bit data groups, verifying the checksum.
    /// </summary>
    /// <param name="source">The encoded string.</param>
    /// <param name="hrp">When this method returns, the lower-case human-readable part.</param>
    /// <param name="data">When this method returns, the 5-bit data groups with the checksum stripped.</param>
    /// <param name="encoding">When this method returns, the scheme whose checksum constant validated the input.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the input mixes case, exceeds the 90-character maximum, lacks the <c>1</c> separator, has an empty
    /// human-readable part, is too short to contain a checksum, contains an out-of-range character, or fails checksum
    /// verification.
    /// </exception>
    public static void Decode(string source, out string hrp, out byte[] data, out Bech32Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(source);
        Decode(source.AsSpan(), out hrp, out data, out encoding);
    }

    /// <summary>
    /// Decodes a Bech32 or Bech32m character span into its human-readable part and 5-bit data groups, verifying the
    /// checksum.
    /// </summary>
    /// <param name="source">The encoded character span.</param>
    /// <param name="hrp">When this method returns, the lower-case human-readable part.</param>
    /// <param name="data">When this method returns, the 5-bit data groups with the checksum stripped.</param>
    /// <param name="encoding">When this method returns, the scheme whose checksum constant validated the input.</param>
    /// <exception cref="FormatException">Thrown when the input is not a valid Bech32 or Bech32m string.</exception>
    public static void Decode(ReadOnlySpan<char> source, out string hrp, out byte[] data, out Bech32Encoding encoding)
    {
        if (!TryDecodeCore(source, out hrp!, out data!, out encoding, out var error))
            throw new FormatException(error);
    }

    /// <summary>
    /// Attempts to decode a Bech32 or Bech32m character span without throwing.
    /// </summary>
    /// <param name="source">The encoded character span.</param>
    /// <param name="hrp">When this method returns <see langword="true" />, the lower-case human-readable part.</param>
    /// <param name="data">
    /// When this method returns <see langword="true" />, the 5-bit data groups with the checksum stripped.
    /// </param>
    /// <param name="encoding">
    /// When this method returns <see langword="true" />, the scheme whose checksum constant validated the input.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the input is a valid Bech32 or Bech32m string; otherwise <see langword="false" />.
    /// </returns>
    public static bool TryDecode(ReadOnlySpan<char> source, [NotNullWhen(true)] out string? hrp, [NotNullWhen(true)] out byte[]? data, out Bech32Encoding encoding) =>
        TryDecodeCore(source, out hrp, out data, out encoding, out _);

    /// <summary>
    /// Decodes a Bech32 or Bech32m string and repacks its 5-bit data groups into 8-bit bytes — the inverse of
    /// <see cref="EncodeFromBytes(string, ReadOnlySpan{byte}, Bech32Encoding)" />.
    /// </summary>
    /// <param name="source">The encoded string.</param>
    /// <param name="hrp">When this method returns, the lower-case human-readable part.</param>
    /// <param name="data">When this method returns, the decoded 8-bit bytes.</param>
    /// <param name="encoding">When this method returns, the scheme whose checksum constant validated the input.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the input is not a valid Bech32 or Bech32m string, or when its 5-bit groups do not repack cleanly
    /// into bytes.
    /// </exception>
    public static void DecodeToBytes(string source, out string hrp, out byte[] data, out Bech32Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(source);

        Decode(source.AsSpan(), out hrp, out var groups, out encoding);
        var bytes = ConvertBits(groups, 5, 8, pad: false);
        data = bytes ?? throw new FormatException(EncodingResourceStrings.Format_Invalid_Bech32DataCharacter);
    }

    /// <summary>
    /// Attempts to decode a Bech32 or Bech32m character span and repack its 5-bit data groups into 8-bit bytes without
    /// throwing.
    /// </summary>
    /// <param name="source">The encoded character span.</param>
    /// <param name="hrp">When this method returns <see langword="true" />, the lower-case human-readable part.</param>
    /// <param name="data">When this method returns <see langword="true" />, the decoded 8-bit bytes.</param>
    /// <param name="encoding">
    /// When this method returns <see langword="true" />, the scheme whose checksum constant validated the input.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the input is valid and its groups repack cleanly into bytes; otherwise
    /// <see langword="false" />.
    /// </returns>
    public static bool TryDecodeToBytes(ReadOnlySpan<char> source, [NotNullWhen(true)] out string? hrp, [NotNullWhen(true)] out byte[]? data, out Bech32Encoding encoding)
    {
        data = null;

        if (!TryDecodeCore(source, out hrp, out var groups, out encoding, out _))
            return false;

        var bytes = ConvertBits(groups, 5, 8, pad: false);
        if (bytes is null)
        {
            hrp = null;
            return false;
        }

        data = bytes;
        return true;
    }

    /// <summary>
    /// Core decoder that splits, validates, and checksum-verifies a Bech32 string.
    /// </summary>
    /// <param name="source">The input span.</param>
    /// <param name="hrp">When this method returns <see langword="true" />, the lower-case human-readable part.</param>
    /// <param name="data">
    /// When this method returns <see langword="true" />, the 5-bit data groups with the checksum stripped.
    /// </param>
    /// <param name="encoding">When this method returns <see langword="true" />, the validating scheme.</param>
    /// <param name="error">When this method returns <see langword="false" />, the failure reason.</param>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    private static bool TryDecodeCore(ReadOnlySpan<char> source, [NotNullWhen(true)] out string? hrp, [NotNullWhen(true)] out byte[]? data, out Bech32Encoding encoding, out string? error)
    {
        hrp = null;
        data = null;
        encoding = default;
        error = null;

        if (source.Length > MaxEncodedLength)
        {
            error = string.Format(System.Globalization.CultureInfo.CurrentCulture, EncodingResourceStrings.Format_Invalid_Bech32TooLong, MaxEncodedLength);
            return false;
        }

        var hasLower = false;
        var hasUpper = false;
        foreach (var c in source)
        {
            if (c is >= 'a' and <= 'z')
                hasLower = true;
            else if (c is >= 'A' and <= 'Z')
                hasUpper = true;
        }

        if (hasLower && hasUpper)
        {
            error = EncodingResourceStrings.Format_Invalid_Bech32MixedCase;
            return false;
        }

        Span<char> lower = source.Length <= 128 ? stackalloc char[source.Length] : new char[source.Length];
        source.ToLowerInvariant(lower);

        var separator = lower.LastIndexOf(Separator);
        if (separator < 0)
        {
            error = EncodingResourceStrings.Format_Invalid_Bech32NoSeparator;
            return false;
        }

        if (separator == 0)
        {
            error = EncodingResourceStrings.Format_Invalid_Bech32EmptyHrp;
            return false;
        }

        var dataPart = lower.Slice(separator + 1);
        if (dataPart.Length < ChecksumSymbols)
        {
            error = EncodingResourceStrings.Format_Invalid_Bech32TooShort;
            return false;
        }

        var hrpPart = lower.Slice(0, separator);
        foreach (var c in hrpPart)
        {
            if (c < MinHrpChar || c > MaxHrpChar)
            {
                error = EncodingResourceStrings.Format_Invalid_Bech32HrpCharacter;
                return false;
            }
        }

        var values = new byte[dataPart.Length];
        for (var i = 0; i < dataPart.Length; i++)
        {
            var c = dataPart[i];
            var value = c < s_charsetReverse.Length ? s_charsetReverse[c] : (sbyte)-1;
            if (value < 0)
            {
                error = string.Format(System.Globalization.CultureInfo.CurrentCulture, EncodingResourceStrings.Format_Invalid_Bech32DataCharacter, c);
                return false;
            }

            values[i] = (byte)value;
        }

        var hrpString = new string(hrpPart);
        if (!VerifyChecksum(hrpString, values, out encoding))
        {
            error = EncodingResourceStrings.Format_Invalid_Bech32Checksum;
            return false;
        }

        hrp = hrpString;
        data = values[..^ChecksumSymbols];
        return true;
    }
}
