// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hkdf.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the HMAC-based Extract-and-Expand Key Derivation Function (HKDF) defined in RFC 5869, exposing the
/// <c>Extract</c>, <c>Expand</c>, and combined <c>DeriveKey</c> stages over the SHA-1 and SHA-2 family of hash
/// algorithms. This class cannot be instantiated.
/// </summary>
/// <remarks>
/// <para>
/// HKDF turns input keying material that is merely high-entropy — such as a Diffie-Hellman shared secret — into one or
/// more cryptographically strong, fixed-length keys. <see cref="Extract" /> concentrates the entropy of the input into
/// a pseudorandom key (PRK) of one hash length; <see cref="Expand" /> stretches that PRK into output keying material of
/// any requested length, optionally bound to an application-specific <c>info</c> context. <see cref="DeriveKey" />
/// performs both stages in one call.
/// </para>
/// <para>
/// The surface mirrors the BCL's <see cref="System.Security.Cryptography.HKDF" /> so the two are interchangeable. The
/// supported hash algorithms are <see cref="HashAlgorithmName.SHA1" />, <see cref="HashAlgorithmName.SHA256" />,
/// <see cref="HashAlgorithmName.SHA384" />, and <see cref="HashAlgorithmName.SHA512" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// byte[] key = Hkdf.DeriveKey(
///     HashAlgorithmName.SHA256,
///     inputKeyingMaterial: sharedSecret,
///     outputLength: 32,
///     salt: salt,
///     info: "app v1 traffic key"u8);
///]]>
/// </code>
/// </example>
public static class Hkdf
{
    /// <summary>
    /// Performs the RFC 5869 extract stage, condensing <paramref name="inputKeyingMaterial" /> into a pseudorandom key
    /// of one hash length.
    /// </summary>
    /// <param name="hashAlgorithm">The HMAC hash algorithm to use.</param>
    /// <param name="inputKeyingMaterial">The input keying material to extract entropy from.</param>
    /// <param name="salt">The optional non-secret salt. When empty, a string of hash-length zero bytes is used.</param>
    /// <returns>The pseudorandom key (PRK), one hash length long.</returns>
    /// <exception cref="ArgumentException"><paramref name="hashAlgorithm" /> is not a supported HKDF hash algorithm.</exception>
    public static byte[] Extract(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> inputKeyingMaterial, ReadOnlySpan<byte> salt = default)
    {
        int hashLength = GetHashLengthInBytes(hashAlgorithm);

        byte[] prk = new byte[hashLength];
        Hmac(hashAlgorithm, salt, inputKeyingMaterial, prk);

        return prk;
    }

    /// <summary>
    /// Performs the RFC 5869 extract stage, writing the pseudorandom key into <paramref name="destination" />.
    /// </summary>
    /// <param name="hashAlgorithm">The HMAC hash algorithm to use.</param>
    /// <param name="inputKeyingMaterial">The input keying material to extract entropy from.</param>
    /// <param name="salt">The optional non-secret salt. When empty, a string of hash-length zero bytes is used.</param>
    /// <param name="destination">The span that receives the pseudorandom key; must be exactly one hash length long.</param>
    /// <returns>The number of bytes written to <paramref name="destination" />, equal to the hash length.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="hashAlgorithm" /> is not a supported HKDF hash algorithm, or <paramref name="destination" /> is
    /// not exactly one hash length long.
    /// </exception>
    public static int Extract(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> inputKeyingMaterial, ReadOnlySpan<byte> salt, Span<byte> destination)
    {
        int hashLength = GetHashLengthInBytes(hashAlgorithm);
        CryptographyThrowHelper.ThrowIfInvalidDestinationLength(destination, hashLength);

        return Hmac(hashAlgorithm, salt, inputKeyingMaterial, destination);
    }

    /// <summary>
    /// Performs the RFC 5869 expand stage, stretching <paramref name="pseudoRandomKey" /> into
    /// <paramref name="outputLength" /> bytes of output keying material.
    /// </summary>
    /// <param name="hashAlgorithm">The HMAC hash algorithm to use.</param>
    /// <param name="pseudoRandomKey">The pseudorandom key produced by <see cref="Extract(HashAlgorithmName, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />.</param>
    /// <param name="outputLength">The number of bytes to produce.</param>
    /// <param name="info">The optional context and application-specific information.</param>
    /// <returns>The output keying material, <paramref name="outputLength" /> bytes long.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="hashAlgorithm" /> is not a supported HKDF hash algorithm, or <paramref name="pseudoRandomKey" />
    /// is shorter than one hash length.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="outputLength" /> is not between 1 and 255 times the hash length.
    /// </exception>
    public static byte[] Expand(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> pseudoRandomKey, int outputLength, ReadOnlySpan<byte> info = default)
    {
        int hashLength = GetHashLengthInBytes(hashAlgorithm);
        ThrowIfInvalidOutputLength(outputLength, hashLength);

        byte[] output = new byte[outputLength];
        ExpandCore(hashAlgorithm, hashLength, pseudoRandomKey, output, info);

        return output;
    }

    /// <summary>
    /// Performs the RFC 5869 expand stage, writing the output keying material into <paramref name="output" />.
    /// </summary>
    /// <param name="hashAlgorithm">The HMAC hash algorithm to use.</param>
    /// <param name="pseudoRandomKey">The pseudorandom key produced by <see cref="Extract(HashAlgorithmName, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />.</param>
    /// <param name="output">The span that receives the output keying material; its length determines how many bytes are produced.</param>
    /// <param name="info">The optional context and application-specific information.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="hashAlgorithm" /> is not a supported HKDF hash algorithm, or <paramref name="pseudoRandomKey" />
    /// is shorter than one hash length.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="output" /> is empty or longer than 255 times the hash length.
    /// </exception>
    public static void Expand(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> pseudoRandomKey, Span<byte> output, ReadOnlySpan<byte> info = default)
    {
        int hashLength = GetHashLengthInBytes(hashAlgorithm);
        ThrowIfInvalidOutputLength(output.Length, hashLength);

        ExpandCore(hashAlgorithm, hashLength, pseudoRandomKey, output, info);
    }

    /// <summary>
    /// Performs the RFC 5869 extract and expand stages in one call, deriving <paramref name="outputLength" /> bytes of
    /// output keying material directly from <paramref name="inputKeyingMaterial" />.
    /// </summary>
    /// <param name="hashAlgorithm">The HMAC hash algorithm to use.</param>
    /// <param name="inputKeyingMaterial">The input keying material to derive from.</param>
    /// <param name="outputLength">The number of bytes to produce.</param>
    /// <param name="salt">The optional non-secret salt. When empty, a string of hash-length zero bytes is used.</param>
    /// <param name="info">The optional context and application-specific information.</param>
    /// <returns>The output keying material, <paramref name="outputLength" /> bytes long.</returns>
    /// <exception cref="ArgumentException"><paramref name="hashAlgorithm" /> is not a supported HKDF hash algorithm.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="outputLength" /> is not between 1 and 255 times the hash length.
    /// </exception>
    public static byte[] DeriveKey(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> inputKeyingMaterial, int outputLength, ReadOnlySpan<byte> salt = default, ReadOnlySpan<byte> info = default)
    {
        int hashLength = GetHashLengthInBytes(hashAlgorithm);
        ThrowIfInvalidOutputLength(outputLength, hashLength);

        byte[] output = new byte[outputLength];
        DeriveKeyCore(hashAlgorithm, hashLength, inputKeyingMaterial, output, salt, info);

        return output;
    }

    /// <summary>
    /// Performs the RFC 5869 extract and expand stages in one call, writing the output keying material into
    /// <paramref name="output" />.
    /// </summary>
    /// <param name="hashAlgorithm">The HMAC hash algorithm to use.</param>
    /// <param name="inputKeyingMaterial">The input keying material to derive from.</param>
    /// <param name="output">The span that receives the output keying material; its length determines how many bytes are produced.</param>
    /// <param name="salt">The optional non-secret salt. When empty, a string of hash-length zero bytes is used.</param>
    /// <param name="info">The optional context and application-specific information.</param>
    /// <exception cref="ArgumentException"><paramref name="hashAlgorithm" /> is not a supported HKDF hash algorithm.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="output" /> is empty or longer than 255 times the hash length.
    /// </exception>
    public static void DeriveKey(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> inputKeyingMaterial, Span<byte> output, ReadOnlySpan<byte> salt = default, ReadOnlySpan<byte> info = default)
    {
        int hashLength = GetHashLengthInBytes(hashAlgorithm);
        ThrowIfInvalidOutputLength(output.Length, hashLength);

        DeriveKeyCore(hashAlgorithm, hashLength, inputKeyingMaterial, output, salt, info);
    }

    /// <summary>
    /// Extracts a pseudorandom key from <paramref name="inputKeyingMaterial" /> and immediately expands it into
    /// <paramref name="output" />, zeroing the intermediate PRK before returning.
    /// </summary>
    /// <param name="hashAlgorithm">The HMAC hash algorithm to use.</param>
    /// <param name="hashLength">The hash length, in bytes, for <paramref name="hashAlgorithm" />.</param>
    /// <param name="inputKeyingMaterial">The input keying material to derive from.</param>
    /// <param name="output">The span that receives the output keying material.</param>
    /// <param name="salt">The optional non-secret salt.</param>
    /// <param name="info">The optional context and application-specific information.</param>
    private static void DeriveKeyCore(HashAlgorithmName hashAlgorithm, int hashLength, ReadOnlySpan<byte> inputKeyingMaterial, Span<byte> output, ReadOnlySpan<byte> salt, ReadOnlySpan<byte> info)
    {
        Span<byte> prk = stackalloc byte[hashLength];
        try
        {
            Hmac(hashAlgorithm, salt, inputKeyingMaterial, prk);
            ExpandCore(hashAlgorithm, hashLength, prk, output, info);
        }
        finally
        {
            CryptographyHelper.Clear(prk);
        }
    }

    /// <summary>
    /// Implements the RFC 5869 expand iteration <c>T(i) = HMAC(PRK, T(i-1) ‖ info ‖ i)</c>, writing the first
    /// <c>output.Length</c> octets of the concatenated <c>T</c> blocks into <paramref name="output" />.
    /// </summary>
    /// <param name="hashAlgorithm">The HMAC hash algorithm to use.</param>
    /// <param name="hashLength">The hash length, in bytes, for <paramref name="hashAlgorithm" />.</param>
    /// <param name="pseudoRandomKey">The pseudorandom key.</param>
    /// <param name="output">The span that receives the output keying material.</param>
    /// <param name="info">The optional context and application-specific information.</param>
    /// <exception cref="ArgumentException"><paramref name="pseudoRandomKey" /> is shorter than one hash length.</exception>
    private static void ExpandCore(HashAlgorithmName hashAlgorithm, int hashLength, ReadOnlySpan<byte> pseudoRandomKey, Span<byte> output, ReadOnlySpan<byte> info)
    {
        if (pseudoRandomKey.Length < hashLength)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_Invalid_HkdfPseudoRandomKeyLength, hashLength),
                nameof(pseudoRandomKey));
        }

        // The HMAC message is T(i-1) ‖ info ‖ counter. The first round omits the (empty) T(0) block.
        int messageLength = hashLength + info.Length + 1;
        byte[] message = ArrayPool<byte>.Shared.Rent(messageLength);
        Span<byte> t = stackalloc byte[hashLength];

        try
        {
            int generated = 0;
            byte counter = 0;

            while (generated < output.Length)
            {
                counter++;

                int offset = 0;
                if (counter > 1)
                {
                    t.CopyTo(message.AsSpan(offset));
                    offset += hashLength;
                }

                info.CopyTo(message.AsSpan(offset));
                offset += info.Length;
                message[offset++] = counter;

                Hmac(hashAlgorithm, pseudoRandomKey, message.AsSpan(0, offset), t);

                int chunk = Math.Min(hashLength, output.Length - generated);
                t[..chunk].CopyTo(output[generated..]);
                generated += chunk;
            }
        }
        finally
        {
            CryptographyHelper.Clear(t);
            CryptographicOperations.ZeroMemory(message.AsSpan(0, messageLength));
            ArrayPool<byte>.Shared.Return(message);
        }
    }

    /// <summary>
    /// Computes an HMAC over <paramref name="source" /> with <paramref name="key" /> for the requested hash algorithm,
    /// writing the result into <paramref name="destination" />.
    /// </summary>
    /// <param name="hashAlgorithm">The HMAC hash algorithm to use.</param>
    /// <param name="key">The HMAC key.</param>
    /// <param name="source">The data to authenticate.</param>
    /// <param name="destination">The span that receives the HMAC.</param>
    /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
    /// <exception cref="ArgumentException"><paramref name="hashAlgorithm" /> is not a supported HKDF hash algorithm.</exception>
    private static int Hmac(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (hashAlgorithm == HashAlgorithmName.SHA256)
            return HMACSHA256.HashData(key, source, destination);
        if (hashAlgorithm == HashAlgorithmName.SHA384)
            return HMACSHA384.HashData(key, source, destination);
        if (hashAlgorithm == HashAlgorithmName.SHA512)
            return HMACSHA512.HashData(key, source, destination);
        if (hashAlgorithm == HashAlgorithmName.SHA1)
            return HMACSHA1.HashData(key, source, destination);

        throw UnsupportedHashAlgorithm(hashAlgorithm);
    }

    /// <summary>
    /// Returns the output length, in bytes, of the HMAC built on <paramref name="hashAlgorithm" />.
    /// </summary>
    /// <param name="hashAlgorithm">The hash algorithm to measure.</param>
    /// <returns>The hash length in bytes.</returns>
    /// <exception cref="ArgumentException"><paramref name="hashAlgorithm" /> is not a supported HKDF hash algorithm.</exception>
    private static int GetHashLengthInBytes(HashAlgorithmName hashAlgorithm)
    {
        if (hashAlgorithm == HashAlgorithmName.SHA256)
            return 32;
        if (hashAlgorithm == HashAlgorithmName.SHA384)
            return 48;
        if (hashAlgorithm == HashAlgorithmName.SHA512)
            return 64;
        if (hashAlgorithm == HashAlgorithmName.SHA1)
            return 20;

        throw UnsupportedHashAlgorithm(hashAlgorithm);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> when the requested output length is outside the RFC 5869
    /// permitted range of 1 to 255 times the hash length.
    /// </summary>
    /// <param name="outputLength">The requested output length, in bytes.</param>
    /// <param name="hashLength">The hash length, in bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="outputLength" /> is less than 1 or greater than <c>255 * hashLength</c>.
    /// </exception>
    private static void ThrowIfInvalidOutputLength(int outputLength, int hashLength)
    {
        int maxOutputLength = 255 * hashLength;
        if (outputLength < 1 || outputLength > maxOutputLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outputLength),
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_OutOfRange_HkdfOutputLength, maxOutputLength));
        }
    }

    /// <summary>
    /// Builds the <see cref="ArgumentException" /> thrown when an unsupported hash algorithm is supplied.
    /// </summary>
    /// <param name="hashAlgorithm">The unsupported hash algorithm.</param>
    /// <returns>An <see cref="ArgumentException" /> describing the failure.</returns>
    private static ArgumentException UnsupportedHashAlgorithm(HashAlgorithmName hashAlgorithm) =>
        new(
            string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_Invalid_HkdfHashAlgorithm, hashAlgorithm.Name),
            nameof(hashAlgorithm));
}
