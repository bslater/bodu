// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmExtensions.Decrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class SymmetricStreamAlgorithmExtensions
{
    /// <summary>
    /// Decrypts the entire contents of a byte array using the specified stream cipher.
    /// </summary>
    /// <param name="algorithm">The stream cipher to use. Must not be <see langword="null" />.</param>
    /// <param name="array">The input byte array to decrypt. Must not be <see langword="null" />.</param>
    /// <returns>A new byte array containing the decrypted output.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" /> or <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Because an additive stream cipher is self-inverse, this performs the identical XOR operation as
    /// <see cref="Encrypt(SymmetricStreamAlgorithm, byte[])" />. Equivalent to calling
    /// <c>Decrypt(array, 0, array.Length)</c>.
    /// </remarks>
    public static byte[] Decrypt(this SymmetricStreamAlgorithm algorithm, byte[] array)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(array);

        return algorithm.Decrypt(array, 0, array.Length);
    }

    /// <summary>
    /// Decrypts a portion of a byte array beginning at the specified offset and continuing to the end of the array.
    /// </summary>
    /// <param name="algorithm">The stream cipher to use. Must not be <see langword="null" />.</param>
    /// <param name="array">The input byte array to decrypt. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="array" /> at which to begin reading.</param>
    /// <returns>A new byte array containing the decrypted output.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" /> or <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="offset" /> is negative or exceeds the length of <paramref name="array" />.
    /// </exception>
    public static byte[] Decrypt(this SymmetricStreamAlgorithm algorithm, byte[] array, int offset)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(array);

        return algorithm.Decrypt(array, offset, array.Length - offset);
    }

    /// <summary>
    /// Decrypts a contiguous region of a byte array using the specified stream cipher.
    /// </summary>
    /// <param name="algorithm">The stream cipher to use. Must not be <see langword="null" />.</param>
    /// <param name="array">The input byte array to decrypt. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="array" /> at which to begin reading.</param>
    /// <param name="count">The number of bytes to decrypt.</param>
    /// <returns>A new byte array containing the decrypted output.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" /> or <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="offset" /> or <paramref name="count" /> is negative, or the range defined by
    /// <paramref name="offset" /> and <paramref name="count" /> exceeds the bounds of <paramref name="array" />.
    /// </exception>
    public static byte[] Decrypt(this SymmetricStreamAlgorithm algorithm, byte[] array, int offset, int count)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);

        return algorithm.Decrypt(array.AsSpan(offset, count));
    }

    /// <summary>
    /// Decrypts a read-only span of bytes using the specified stream cipher.
    /// </summary>
    /// <param name="algorithm">The stream cipher to use. Must not be <see langword="null" />.</param>
    /// <param name="input">The span of input bytes to decrypt.</param>
    /// <returns>A new byte array containing the decrypted output.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="algorithm" /> is <see langword="null" />.</exception>
    public static byte[] Decrypt(this SymmetricStreamAlgorithm algorithm, ReadOnlySpan<byte> input)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        using ICryptoTransform transform = algorithm.CreateDecryptor();
        return transform.Transform(input);
    }

    /// <summary>
    /// Decrypts a read-only memory region using the specified stream cipher.
    /// </summary>
    /// <param name="algorithm">The stream cipher to use. Must not be <see langword="null" />.</param>
    /// <param name="input">The memory region containing the bytes to decrypt.</param>
    /// <returns>A new byte array containing the decrypted output.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="algorithm" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// Delegates to <see cref="Decrypt(SymmetricStreamAlgorithm, ReadOnlySpan{byte})" />.
    /// </remarks>
    public static byte[] Decrypt(this SymmetricStreamAlgorithm algorithm, ReadOnlyMemory<byte> input) =>
        algorithm.Decrypt(input.Span);

    /// <summary>
    /// Decrypts data read from a source stream and writes the decrypted output to a target stream, using the default
    /// buffer size.
    /// </summary>
    /// <param name="algorithm">The stream cipher to use. Must not be <see langword="null" />.</param>
    /// <param name="sourceStream">The stream to read ciphertext from. Must not be <see langword="null" />.</param>
    /// <param name="targetStream">
    /// The stream to write the decrypted output to. Must not be <see langword="null" />.
    /// </param>
    /// <returns>The total number of ciphertext bytes read from <paramref name="sourceStream" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" />, <paramref name="sourceStream" />, or <paramref name="targetStream" /> is
    /// <see langword="null" />.
    /// </exception>
    public static int Decrypt(this SymmetricStreamAlgorithm algorithm, Stream sourceStream, Stream targetStream) =>
        algorithm.Decrypt(sourceStream, targetStream, SymmetricAlgorithmExtensions.DefaultBufferSize);

    /// <summary>
    /// Decrypts data read from a source stream and writes the decrypted output to a target stream, using the specified
    /// buffer size.
    /// </summary>
    /// <param name="algorithm">The stream cipher to use. Must not be <see langword="null" />.</param>
    /// <param name="sourceStream">The stream to read ciphertext from. Must not be <see langword="null" />.</param>
    /// <param name="targetStream">
    /// The stream to write the decrypted output to. Must not be <see langword="null" />.
    /// </param>
    /// <param name="bufferSize">The size, in bytes, of the read buffer.</param>
    /// <returns>The total number of ciphertext bytes read from <paramref name="sourceStream" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" />, <paramref name="sourceStream" />, or <paramref name="targetStream" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="bufferSize" /> is less than or equal to zero.
    /// </exception>
    public static int Decrypt(this SymmetricStreamAlgorithm algorithm, Stream sourceStream, Stream targetStream, int bufferSize)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(sourceStream);
        ThrowHelper.ThrowIfNull(targetStream);
        ThrowHelper.ThrowIfLessThanOrEqual(bufferSize, 0);

        using ICryptoTransform transform = algorithm.CreateDecryptor();
        return transform.Transform(sourceStream, targetStream, bufferSize);
    }
}
