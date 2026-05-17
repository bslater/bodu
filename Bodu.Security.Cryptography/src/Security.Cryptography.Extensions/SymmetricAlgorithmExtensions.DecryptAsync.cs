// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmExtensions.DecryptAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class SymmetricAlgorithmExtensions
{
    /// <summary>
    /// Asynchronously decrypts data read from a source stream and writes the decrypted output to a target stream, using
    /// the default buffer size.
    /// </summary>
    /// <param name="algorithm">
    /// The symmetric algorithm to use for decryption. Must not be <see langword="null" />.
    /// </param>
    /// <param name="sourceStream">The stream to read encrypted data from. Must not be <see langword="null" />.</param>
    /// <param name="targetStream">
    /// The stream to write the decrypted output to. Must not be <see langword="null" />.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous decryption operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" />, <paramref name="sourceStream" />, or <paramref name="targetStream" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled via <paramref name="cancellationToken" />.
    /// </exception>
    public static Task DecryptAsync(
        this SymmetricAlgorithm algorithm,
        Stream sourceStream,
        Stream targetStream,
        CancellationToken cancellationToken = default) =>
        algorithm.DecryptAsync(sourceStream, targetStream, DefaultBufferSize, cancellationToken);

    /// <summary>
    /// Asynchronously decrypts data read from a source stream and writes the decrypted output to a target stream, using
    /// the specified buffer size.
    /// </summary>
    /// <param name="algorithm">
    /// The symmetric algorithm to use for decryption. Must not be <see langword="null" />.
    /// </param>
    /// <param name="sourceStream">The stream to read encrypted data from. Must not be <see langword="null" />.</param>
    /// <param name="targetStream">
    /// The stream to write the decrypted output to. Must not be <see langword="null" />.
    /// </param>
    /// <param name="bufferSize">The size, in bytes, of the read buffer. Must be greater than zero.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous decryption operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" />, <paramref name="sourceStream" />, or <paramref name="targetStream" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="bufferSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <example>
    /// <code>
    ///<![CDATA[
    /// using var aes = Aes.Create();
    /// await aes.DecryptAsync(encryptedStream, decryptedStream, 4096, cancellationToken);
    ///]]>
    /// </code>
    /// </example>
    public static async Task DecryptAsync(
        this SymmetricAlgorithm algorithm,
        Stream sourceStream,
        Stream targetStream,
        int bufferSize,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(sourceStream);
        ThrowHelper.ThrowIfNull(targetStream);
        ThrowHelper.ThrowIfLessThanOrEqual(bufferSize, 0);

        using ICryptoTransform transform = algorithm.CreateDecryptor();
        await transform.TransformAsync(sourceStream, targetStream, bufferSize, cancellationToken)
            .ConfigureAwait(false);
    }
}
