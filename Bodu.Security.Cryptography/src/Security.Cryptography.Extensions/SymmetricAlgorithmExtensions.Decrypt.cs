// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmExtensions.Decrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Security.Cryptography;
using Bodu;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class SymmetricAlgorithmExtensions
{
    /// <summary>
    /// Decrypts the entire contents of a byte array using the specified symmetric algorithm.
    /// </summary>
    /// <param name="algorithm">The symmetric algorithm to use for decryption. Must not be <see langword="null" />.</param>
    /// <param name="array">The input byte array to decrypt. Must not be <see langword="null" />.</param>
    /// <returns>A new byte array containing the decrypted output.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" /> is <see langword="null" />.
    /// <para>-or-</para>
    /// <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>Equivalent to calling <c>Decrypt(array, 0, array.Length)</c>.</remarks>
    public static byte[] Decrypt(this SymmetricAlgorithm algorithm, byte[] array)
        => algorithm.Decrypt(array, 0, array?.Length ?? 0);

    /// <summary>
    /// Decrypts a portion of a byte array beginning at the specified offset and continuing to the end of the array.
    /// </summary>
    /// <param name="algorithm">The symmetric algorithm to use for decryption. Must not be <see langword="null" />.</param>
    /// <param name="array">The input byte array to decrypt. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="array" /> at which to begin reading.</param>
    /// <returns>A new byte array containing the decrypted output.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" /> is <see langword="null" />.
    /// <para>-or-</para>
    /// <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="offset" /> is negative or exceeds the length of <paramref name="array" />.
    /// </exception>
    public static byte[] Decrypt(this SymmetricAlgorithm algorithm, byte[] array, int offset)
        => algorithm.Decrypt(array, offset, (array?.Length - offset) ?? 0);

    /// <summary>
    /// Decrypts a contiguous region of a byte array using the specified symmetric algorithm.
    /// </summary>
    /// <param name="algorithm">The symmetric algorithm to use for decryption. Must not be <see langword="null" />.</param>
    /// <param name="array">The input byte array to decrypt. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="array" /> at which to begin reading.</param>
    /// <param name="count">The number of bytes to decrypt.</param>
    /// <returns>A new byte array containing the decrypted output.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" /> is <see langword="null" />.
    /// <para>-or-</para>
    /// <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="offset" /> or <paramref name="count" /> is negative, or the range defined by <paramref name="offset" /> and
    /// <paramref name="count" /> exceeds the bounds of <paramref name="array" />.
    /// </exception>
    /// <example>
    /// <code>
    ///<![CDATA[
    /// using var aes = Aes.Create();
    /// byte[] decrypted = aes.Decrypt(cipherText, 0, cipherText.Length);
    ///]]>
    /// </code>
    /// </example>
    public static byte[] Decrypt(this SymmetricAlgorithm algorithm, byte[] array, int offset, int count)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);

        return algorithm.Decrypt(array.AsSpan(offset, count));
    }

    /// <summary>
    /// Decrypts a read-only span of bytes using the specified symmetric algorithm.
    /// </summary>
    /// <param name="algorithm">The symmetric algorithm to use for decryption. Must not be <see langword="null" />.</param>
    /// <param name="input">The span of input bytes to decrypt.</param>
    /// <returns>A new byte array containing the decrypted output.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    public static byte[] Decrypt(this SymmetricAlgorithm algorithm, ReadOnlySpan<byte> input)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        using ICryptoTransform transform = algorithm.CreateDecryptor();
        return transform.Transform(input);
    }

    /// <summary>
    /// Decrypts a read-only memory region using the specified symmetric algorithm.
    /// </summary>
    /// <param name="algorithm">The symmetric algorithm to use for decryption. Must not be <see langword="null" />.</param>
    /// <param name="input">The memory region containing the bytes to decrypt.</param>
    /// <returns>A new byte array containing the decrypted output.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>Delegates to <see cref="Decrypt(SymmetricAlgorithm, ReadOnlySpan{byte})" />.</remarks>
    public static byte[] Decrypt(this SymmetricAlgorithm algorithm, ReadOnlyMemory<byte> input)
        => algorithm.Decrypt(input.Span);

    /// <summary>
    /// Decrypts data read from a source stream and writes the decrypted output to a target stream, using the default buffer size.
    /// </summary>
    /// <param name="algorithm">The symmetric algorithm to use for decryption. Must not be <see langword="null" />.</param>
    /// <param name="sourceStream">The stream to read encrypted data from. Must not be <see langword="null" />.</param>
    /// <param name="targetStream">The stream to write the decrypted output to. Must not be <see langword="null" />.</param>
    /// <returns>The total number of encrypted bytes read from <paramref name="sourceStream" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" />, <paramref name="sourceStream" />, or <paramref name="targetStream" /> is <see langword="null" />.
    /// </exception>
    public static int Decrypt(this SymmetricAlgorithm algorithm, Stream sourceStream, Stream targetStream)
        => algorithm.Decrypt(sourceStream, targetStream, DefaultBufferSize);

    /// <summary>
    /// Decrypts data read from a source stream and writes the decrypted output to a target stream, using the specified buffer size.
    /// </summary>
    /// <param name="algorithm">The symmetric algorithm to use for decryption. Must not be <see langword="null" />.</param>
    /// <param name="sourceStream">The stream to read encrypted data from. Must not be <see langword="null" />.</param>
    /// <param name="targetStream">The stream to write the decrypted output to. Must not be <see langword="null" />.</param>
    /// <param name="bufferSize">The size, in bytes, of the read buffer. Must be greater than zero.</param>
    /// <returns>The total number of encrypted bytes read from <paramref name="sourceStream" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" />, <paramref name="sourceStream" />, or <paramref name="targetStream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="bufferSize" /> is less than or equal to zero.
    /// </exception>
    /// <example>
    /// <code>
    ///<![CDATA[
    /// using var aes = Aes.Create();
    /// using var input = File.OpenRead("output.enc");
    /// using var output = File.Create("decrypted.txt");
    /// int bytesRead = aes.Decrypt(input, output, 4096);
    ///]]>
    /// </code>
    /// </example>
    public static int Decrypt(this SymmetricAlgorithm algorithm, Stream sourceStream, Stream targetStream, int bufferSize)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(sourceStream);
        ThrowHelper.ThrowIfNull(targetStream);
        ThrowHelper.ThrowIfLessThanOrEqual(bufferSize, 0);

        using ICryptoTransform transform = algorithm.CreateDecryptor();
        return transform.Transform(sourceStream, targetStream, bufferSize);
    }
}
