// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Check.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Security.Cryptography;

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Base58Check encoding and decoding — a Base58 superset used by Bitcoin addresses, WIF private keys, and
/// related protocols. The encoder appends a four-byte checksum derived from the leading bytes of
/// <c>SHA-256(SHA-256(payload))</c> so that mistyped or truncated input can be detected at decode time.
/// </summary>
/// <remarks>
/// <para>
/// The encoded form is <c>Base58(payload || SHA-256(SHA-256(payload))[0..4])</c>. The decoded form recovers the
/// original <c>payload</c> after verifying the checksum; if the checksum disagrees the decoder fails.
/// </para>
/// <para>
/// Leading zero bytes in the payload are preserved by the underlying Base58 encoder as leading alphabet[0] characters
/// (typically <c>1</c> in the Bitcoin/Flickr alphabet).
/// </para>
/// </remarks>
public static class Base58Check
{

    /// <summary>
    /// The number of checksum bytes appended to the payload before Base58 encoding.
    /// </summary>
    private const int ChecksumLength = 4;

    /// <summary>
    /// Decodes a Base58Check encoded string back to the original payload, throwing if the trailing four-byte checksum
    /// does not match.
    /// </summary>
    /// <param name="source">The Base58Check encoded input.</param>
    /// <param name="variant">The Base58 variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>The decoded payload bytes (the trailing checksum is verified and stripped).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="source" /> contains characters outside the variant alphabet, decodes to fewer than
    /// four bytes, or whose trailing four-byte checksum disagrees with the computed value.
    /// </exception>
    public static byte[] Decode(ReadOnlySpan<char> source, Base58Variant variant = Base58Variant.BitcoinFlickr, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        var decoded = Base58.Decode(source, variant, styles);
        if (decoded.Length < ChecksumLength)
            throw new FormatException(EncodingResourceStrings.Format_Invalid_Base58CheckShorterThanChecksum);

        var payloadLength = decoded.Length - ChecksumLength;
        Span<byte> expectedChecksum = stackalloc byte[ChecksumLength];
        ComputeChecksum(decoded.AsSpan(0, payloadLength), expectedChecksum);

        if (!decoded.AsSpan(payloadLength, ChecksumLength).SequenceEqual(expectedChecksum))
            throw new FormatException(EncodingResourceStrings.Format_Invalid_Base58CheckChecksumFailed);

        var payload = new byte[payloadLength];
        Buffer.BlockCopy(decoded, 0, payload, 0, payloadLength);
        return payload;
    }

    /// <summary>
    /// Encodes <paramref name="payload" /> using Base58Check — appends a four-byte SHA-256 squared checksum and Base58
    /// encodes the concatenation.
    /// </summary>
    /// <param name="payload">The bytes to encode.</param>
    /// <param name="variant">The Base58 variant.</param>
    /// <returns>A Base58Check encoded string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static string Encode(ReadOnlySpan<byte> payload, Base58Variant variant = Base58Variant.BitcoinFlickr)
    {
        var totalLength = payload.Length + ChecksumLength;
        var rented = ArrayPool<byte>.Shared.Rent(totalLength);
        try
        {
            Span<byte> buffer = rented.AsSpan(0, totalLength);
            payload.CopyTo(buffer);
            ComputeChecksum(payload, buffer.Slice(payload.Length));
            return Base58.Encode(buffer, variant);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Returns an upper bound on the payload byte count that decoding <paramref name="charCount" /> characters can
    /// produce, after stripping the four-byte checksum suffix.
    /// </summary>
    /// <param name="charCount">The input character count.</param>
    /// <returns>
    /// An upper bound on the payload byte count, or <c>0</c> when <paramref name="charCount" /> is too small to contain
    /// a checksum.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="charCount" /> is negative.</exception>
    public static int GetMaxDecodedLength(int charCount)
    {
        ThrowHelper.ThrowIfNegative(charCount);
        var maxDecoded = Base58.GetMaxDecodedLength(charCount);
        return maxDecoded <= ChecksumLength ? 0 : maxDecoded - ChecksumLength;
    }

    /// <summary>
    /// Returns an upper bound on the number of characters required to encode a <paramref name="payloadByteCount" />
    /// -byte payload via Base58Check (payload + four checksum bytes).
    /// </summary>
    /// <param name="payloadByteCount">The payload byte count.</param>
    /// <returns>An upper bound on the encoded character count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="payloadByteCount" /> is negative.
    /// </exception>
    public static int GetMaxEncodedLength(int payloadByteCount)
    {
        ThrowHelper.ThrowIfNegative(payloadByteCount);
        return Base58.GetMaxEncodedLength(payloadByteCount + ChecksumLength);
    }

    /// <summary>
    /// Indicates whether <paramref name="source" /> is a valid Base58Check input under the supplied variant — that is,
    /// every character is in the variant alphabet and the trailing four-byte checksum matches the decoded payload.
    /// </summary>
    /// <param name="source">The Base58Check encoded input.</param>
    /// <param name="variant">The Base58 variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>
    /// <see langword="true" /> when the input is a structurally and cryptographically valid Base58Check encoding;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool IsValid(ReadOnlySpan<char> source, Base58Variant variant = Base58Variant.BitcoinFlickr, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        if (!Base58.IsValid(source, variant, styles))
            return false;

        var upper = Base58.GetMaxDecodedLength(source.Length);
        var rented = ArrayPool<byte>.Shared.Rent(upper);
        try
        {
            if (!Base58.TryDecode(source, rented, out var decodedLength, variant, styles))
                return false;

            if (decodedLength < ChecksumLength)
                return false;

            var payloadLength = decodedLength - ChecksumLength;
            Span<byte> expectedChecksum = stackalloc byte[ChecksumLength];
            ComputeChecksum(rented.AsSpan(0, payloadLength), expectedChecksum);

            return rented.AsSpan(payloadLength, ChecksumLength).SequenceEqual(expectedChecksum);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Attempts to decode a Base58Check encoded string into <paramref name="destination" />, returning
    /// <see langword="false" /> when the checksum disagrees, the destination is too small, or the input is malformed.
    /// </summary>
    /// <param name="source">The Base58Check encoded input.</param>
    /// <param name="destination">The destination span receiving the payload bytes.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="variant">The Base58 variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>
    /// <see langword="true" /> when decoding succeeds and the checksum verifies; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten, Base58Variant variant = Base58Variant.BitcoinFlickr, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        bytesWritten = 0;
        var upper = Base58.GetMaxDecodedLength(source.Length);
        var rented = ArrayPool<byte>.Shared.Rent(upper);
        try
        {
            if (!Base58.TryDecode(source, rented, out var decodedLength, variant, styles))
                return false;

            if (decodedLength < ChecksumLength)
                return false;

            var payloadLength = decodedLength - ChecksumLength;
            Span<byte> expectedChecksum = stackalloc byte[ChecksumLength];
            ComputeChecksum(rented.AsSpan(0, payloadLength), expectedChecksum);

            if (!rented.AsSpan(payloadLength, ChecksumLength).SequenceEqual(expectedChecksum))
                return false;

            if (destination.Length < payloadLength)
                return false;

            rented.AsSpan(0, payloadLength).CopyTo(destination);
            bytesWritten = payloadLength;
            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Computes the first four bytes of <c>SHA-256(SHA-256(payload))</c> into <paramref name="destination" />.
    /// </summary>
    /// <param name="payload">The payload bytes.</param>
    /// <param name="destination">The destination span; must be exactly <see cref="ChecksumLength" /> bytes.</param>
    private static void ComputeChecksum(ReadOnlySpan<byte> payload, Span<byte> destination)
    {
        Span<byte> firstHash = stackalloc byte[32];
        Span<byte> secondHash = stackalloc byte[32];
        SHA256.HashData(payload, firstHash);
        SHA256.HashData(firstHash, secondHash);
        secondHash.Slice(0, ChecksumLength).CopyTo(destination);
    }
}
