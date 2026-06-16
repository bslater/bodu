// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensions.VerifyHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using System.Text;

namespace Bodu.IO.Hashing.Extensions;

public static partial class NonCryptographicHashAlgorithmExtensions
{
    /// <summary>
    /// Verifies that the computed hash of the input data matches the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="input">
    /// The input byte array whose hash will be computed. Must not be <see langword="null" />.
    /// </param>
    /// <param name="expectedHash">The expected hash value as a byte array. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the computed hash equals <paramref name="expectedHash" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" />, <paramref name="input" />, or <paramref name="expectedHash" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The algorithm state is reset before computation and restored to a clean state after
    /// <see cref="NonCryptographicHashAlgorithm.GetHashAndReset()" /> completes. Any prior incremental state is
    /// discarded.
    /// </remarks>
    /// <remarks>
    /// Non-cryptographic hash algorithms are designed for scenarios such as checksums, hash tables, sharding,
    /// bucketing, fingerprinting, and accidental-corruption detection. They must not be used for password hashing,
    /// digital signatures, message authentication, tamper detection, or other security-sensitive purposes.
    /// </remarks>
    /// <remarks>
    /// Comparison uses <see cref="System.MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})" /> and
    /// short-circuits on the first mismatching byte. It is <strong>not</strong> constant-time and must not be used to
    /// verify an authenticator value supplied by an untrusted caller. For constant-time hash verification see the
    /// <c>Bodu.Security.Cryptography.HashAlgorithmExtensions.VerifyHash</c> overloads, which rely on
    /// <see cref="System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// .
    /// </remarks>
    public static bool VerifyHash(this NonCryptographicHashAlgorithm algorithm, byte[] input, byte[] expectedHash)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(expectedHash);

        algorithm.Reset();
        algorithm.Append(input);
        byte[] actualHash = algorithm.GetHashAndReset();

        return actualHash.AsSpan().SequenceEqual(expectedHash);
    }

    /// <summary>
    /// Verifies that the computed hash of the input data matches the expected hash value expressed as a hexadecimal
    /// string.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="input">
    /// The input byte array whose hash will be computed. Must not be <see langword="null" />.
    /// </param>
    /// <param name="expectedHex">
    /// The expected hash value as a hexadecimal string. Case-insensitive. Must not be <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the computed hash matches <paramref name="expectedHex" />; otherwise,
    /// <see langword="false" />. Returns <see langword="false" /> if <paramref name="expectedHex" /> is not a valid
    /// hexadecimal string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" />, <paramref name="input" />, or <paramref name="expectedHex" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <paramref name="expectedHex" /> is decoded to bytes before the hash is computed so that a malformed hex string
    /// fails fast without performing unnecessary work.
    /// </para>
    /// <para>
    /// A malformed <paramref name="expectedHex" /> string (one that cannot be decoded) is treated as a non-match and
    /// returns <see langword="false" />.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Non-cryptographic hash algorithms are designed for scenarios such as checksums, hash tables, sharding,
    /// bucketing, fingerprinting, and accidental-corruption detection. They must not be used for password hashing,
    /// digital signatures, message authentication, tamper detection, or other security-sensitive purposes.
    /// </remarks>
    /// <remarks>
    /// Comparison uses <see cref="System.MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})" /> and
    /// short-circuits on the first mismatching byte. It is <strong>not</strong> constant-time and must not be used to
    /// verify an authenticator value supplied by an untrusted caller. For constant-time hash verification see the
    /// <c>Bodu.Security.Cryptography.HashAlgorithmExtensions.VerifyHash</c> overloads, which rely on
    /// <see cref="System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// .
    /// </remarks>
    public static bool VerifyHash(this NonCryptographicHashAlgorithm algorithm, byte[] input, string expectedHex)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(expectedHex);

        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromHexString(expectedHex);
        }
        catch (FormatException)
        {
            return false;
        }

        algorithm.Reset();
        algorithm.Append(input);
        byte[] actualHash = algorithm.GetHashAndReset();

        return actualHash.AsSpan().SequenceEqual(expectedBytes);
    }

    /// <summary>
    /// Verifies that the computed hash of the stream matches the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="stream">
    /// The input stream to read and hash. Must not be <see langword="null" /> and must be readable.
    /// </param>
    /// <param name="expectedHash">The expected hash value as a byte array. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the hash of the stream matches <paramref name="expectedHash" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" />, <paramref name="stream" />, or <paramref name="expectedHash" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The algorithm state is reset before the stream is read. Any prior incremental state is discarded.
    /// </remarks>
    /// <remarks>
    /// Non-cryptographic hash algorithms are designed for scenarios such as checksums, hash tables, sharding,
    /// bucketing, fingerprinting, and accidental-corruption detection. They must not be used for password hashing,
    /// digital signatures, message authentication, tamper detection, or other security-sensitive purposes.
    /// </remarks>
    /// <remarks>
    /// Comparison uses <see cref="System.MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})" /> and
    /// short-circuits on the first mismatching byte. It is <strong>not</strong> constant-time and must not be used to
    /// verify an authenticator value supplied by an untrusted caller. For constant-time hash verification see the
    /// <c>Bodu.Security.Cryptography.HashAlgorithmExtensions.VerifyHash</c> overloads, which rely on
    /// <see cref="System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// .
    /// </remarks>
    public static bool VerifyHash(this NonCryptographicHashAlgorithm algorithm, Stream stream, byte[] expectedHash)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(expectedHash);

        algorithm.Reset();
        algorithm.AppendData(stream);
        byte[] actualHash = algorithm.GetHashAndReset();

        return actualHash.AsSpan().SequenceEqual(expectedHash);
    }

    /// <summary>
    /// Verifies that the computed hash of the stream matches the expected hash value expressed as a hexadecimal string.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="stream">
    /// The input stream to read and hash. Must not be <see langword="null" /> and must be readable.
    /// </param>
    /// <param name="expectedHex">
    /// The expected hash value as a hexadecimal string. Case-insensitive. Must not be <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the hash of the stream matches <paramref name="expectedHex" />; otherwise,
    /// <see langword="false" />. Returns <see langword="false" /> if <paramref name="expectedHex" /> is not a valid
    /// hexadecimal string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" />, <paramref name="stream" />, or <paramref name="expectedHex" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <paramref name="expectedHex" /> is decoded to bytes before the stream is read so that a malformed hex string
    /// fails fast without consuming stream data or wasting the hash computation.
    /// </para>
    /// <para>
    /// A malformed <paramref name="expectedHex" /> string is treated as a non-match and returns
    /// <see langword="false" />.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Non-cryptographic hash algorithms are designed for scenarios such as checksums, hash tables, sharding,
    /// bucketing, fingerprinting, and accidental-corruption detection. They must not be used for password hashing,
    /// digital signatures, message authentication, tamper detection, or other security-sensitive purposes.
    /// </remarks>
    /// <remarks>
    /// Comparison uses <see cref="System.MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})" /> and
    /// short-circuits on the first mismatching byte. It is <strong>not</strong> constant-time and must not be used to
    /// verify an authenticator value supplied by an untrusted caller. For constant-time hash verification see the
    /// <c>Bodu.Security.Cryptography.HashAlgorithmExtensions.VerifyHash</c> overloads, which rely on
    /// <see cref="System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// .
    /// </remarks>
    public static bool VerifyHash(this NonCryptographicHashAlgorithm algorithm, Stream stream, string expectedHex)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(expectedHex);

        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromHexString(expectedHex);
        }
        catch (FormatException)
        {
            return false;
        }

        algorithm.Reset();
        algorithm.AppendData(stream);
        byte[] actualHash = algorithm.GetHashAndReset();

        return actualHash.AsSpan().SequenceEqual(expectedBytes);
    }

    /// <summary>
    /// Verifies that the computed hash of the input span matches the expected hash span.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> used to compute the hash. Must not be <see langword="null" />.
    /// </param>
    /// <param name="input">The input span of bytes to hash.</param>
    /// <param name="expectedHash">The expected hash as a read-only span of bytes.</param>
    /// <returns>
    /// <see langword="true" /> if the computed hash equals <paramref name="expectedHash" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The algorithm state is reset before computation and restored to a clean state via
    /// <see cref="NonCryptographicHashAlgorithm.GetHashAndReset()" /> after the digest is produced.
    /// </remarks>
    /// <remarks>
    /// Non-cryptographic hash algorithms are designed for scenarios such as checksums, hash tables, sharding,
    /// bucketing, fingerprinting, and accidental-corruption detection. They must not be used for password hashing,
    /// digital signatures, message authentication, tamper detection, or other security-sensitive purposes.
    /// </remarks>
    /// <remarks>
    /// Comparison uses <see cref="System.MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})" /> and
    /// short-circuits on the first mismatching byte. It is <strong>not</strong> constant-time and must not be used to
    /// verify an authenticator value supplied by an untrusted caller. For constant-time hash verification see the
    /// <c>Bodu.Security.Cryptography.HashAlgorithmExtensions.VerifyHash</c> overloads, which rely on
    /// <see cref="System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// .
    /// </remarks>
    public static bool VerifyHash(
        this NonCryptographicHashAlgorithm algorithm,
        ReadOnlySpan<byte> input,
        ReadOnlySpan<byte> expectedHash)
    {
        ArgumentNullException.ThrowIfNull(algorithm);

        algorithm.Reset();
        algorithm.Append(input);
        byte[] actualHash = algorithm.GetHashAndReset();

        return actualHash.AsSpan().SequenceEqual(expectedHash);
    }

    /// <summary>
    /// Verifies that the computed hash of the input memory block matches the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> used to compute the hash. Must not be <see langword="null" />.
    /// </param>
    /// <param name="input">The memory buffer containing the input data to hash.</param>
    /// <param name="expectedHash">The expected hash value as a byte array. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the hash of <paramref name="input" /> equals <paramref name="expectedHash" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> or <paramref name="expectedHash" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Delegates to the
    /// <see cref="VerifyHash(NonCryptographicHashAlgorithm, ReadOnlySpan{byte}, ReadOnlySpan{byte})" /> overload.
    /// </remarks>
    /// <remarks>
    /// Non-cryptographic hash algorithms are designed for scenarios such as checksums, hash tables, sharding,
    /// bucketing, fingerprinting, and accidental-corruption detection. They must not be used for password hashing,
    /// digital signatures, message authentication, tamper detection, or other security-sensitive purposes.
    /// </remarks>
    /// <remarks>
    /// Comparison uses <see cref="System.MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})" /> and
    /// short-circuits on the first mismatching byte. It is <strong>not</strong> constant-time and must not be used to
    /// verify an authenticator value supplied by an untrusted caller. For constant-time hash verification see the
    /// <c>Bodu.Security.Cryptography.HashAlgorithmExtensions.VerifyHash</c> overloads, which rely on
    /// <see cref="System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// .
    /// </remarks>
    public static bool VerifyHash(
        this NonCryptographicHashAlgorithm algorithm,
        ReadOnlyMemory<byte> input,
        byte[] expectedHash)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(expectedHash);

        return algorithm.VerifyHash(input.Span, expectedHash);
    }

    /// <summary>
    /// Verifies that the computed hash of the encoded string matches the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> used to compute the hash. Must not be <see langword="null" />.
    /// </param>
    /// <param name="text">The input string to encode and hash. Must not be <see langword="null" />.</param>
    /// <param name="encoding">
    /// The encoding used to convert <paramref name="text" /> to bytes. Must not be <see langword="null" />.
    /// </param>
    /// <param name="expectedHash">The expected hash as a byte array. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the hash of the encoded string equals <paramref name="expectedHash" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" />, <paramref name="text" />, <paramref name="encoding" />, or
    /// <paramref name="expectedHash" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Non-cryptographic hash algorithms are designed for scenarios such as checksums, hash tables, sharding,
    /// bucketing, fingerprinting, and accidental-corruption detection. They must not be used for password hashing,
    /// digital signatures, message authentication, tamper detection, or other security-sensitive purposes.
    /// </remarks>
    /// <remarks>
    /// Comparison uses <see cref="System.MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})" /> and
    /// short-circuits on the first mismatching byte. It is <strong>not</strong> constant-time and must not be used to
    /// verify an authenticator value supplied by an untrusted caller. For constant-time hash verification see the
    /// <c>Bodu.Security.Cryptography.HashAlgorithmExtensions.VerifyHash</c> overloads, which rely on
    /// <see cref="System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// .
    /// </remarks>
    public static bool VerifyHash(
        this NonCryptographicHashAlgorithm algorithm,
        string text,
        Encoding encoding,
        byte[] expectedHash)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(encoding);
        ArgumentNullException.ThrowIfNull(expectedHash);

        byte[] data = encoding.GetBytes(text);

        return algorithm.VerifyHash(data, expectedHash);
    }
}
