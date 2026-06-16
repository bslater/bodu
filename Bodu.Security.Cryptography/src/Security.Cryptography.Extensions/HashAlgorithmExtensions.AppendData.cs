// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensions.AppendData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class HashAlgorithmExtensions
{
    /// <summary>
    /// Feeds a span of bytes into the ongoing hash computation of the specified <see cref="HashAlgorithm" /> without
    /// finalizing it.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="HashAlgorithm" /> instance receiving the data. Must not be <see langword="null" />.
    /// </param>
    /// <param name="data">The span of bytes to feed into the hash computation.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is intended for use in incremental hashing scenarios where data is supplied in multiple segments.
    /// The caller is responsible for calling <see cref="HashAlgorithm.TransformFinalBlock" /> to complete the hash and
    /// obtain the result.
    /// </para>
    /// <para>
    /// An <see cref="ArrayPool{T}" /> buffer is used internally to bridge the span into the array-based
    /// <see cref="HashAlgorithm.TransformBlock" /> API. The buffer is cleared and returned to the pool in a
    /// <c>finally</c> block so the input data cannot be observed by a subsequent pool consumer even if
    /// <see cref="HashAlgorithm.TransformBlock" /> throws.
    /// </para>
    /// <para>
    /// If <paramref name="data" /> is empty, this method returns without performing any work.
    /// </para>
    /// </remarks>
    public static void AppendData(this HashAlgorithm algorithm, ReadOnlySpan<byte> data)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        // Nothing to feed; skip renting a buffer entirely.
        if (data.IsEmpty)
            return;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(data.Length);
        try
        {
            data.CopyTo(buffer);
            algorithm.TransformBlock(buffer, 0, data.Length, null, 0);
        }
        finally
        {
            // The rented buffer may contain a copy of the caller's input (potentially sensitive, e.g. a password).
            // Clear the used region before returning the array to the shared pool so a subsequent renter cannot
            // observe the plaintext. 'clearArray: true' also zeros the entire rented array.
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }
}
