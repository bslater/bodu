// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="HashAlgorithmExtensions_AppendData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Extensions
{
    using System;
    using System.Buffers;
    using System.Security.Cryptography;

    public static partial class HashAlgorithmExtensions
    {
        /// <summary>
        /// Feeds a span of bytes into the ongoing hash computation of the specified <see cref="HashAlgorithm" /> without finalising it.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> instance receiving the data. Must not be <see langword="null" />.</param>
        /// <param name="data">The span of bytes to feed into the hash computation.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method is intended for use in incremental hashing scenarios where data is supplied in multiple segments. The caller is
        /// responsible for calling <see cref="HashAlgorithm.TransformFinalBlock" /> to complete the hash and obtain the result.
        /// </para>
        /// <para>
        /// An <see cref="ArrayPool{T}" /> buffer is used internally to bridge the span into the array-based
        /// <see cref="HashAlgorithm.TransformBlock" /> API. The buffer is always returned to the pool, even if an exception occurs.
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
                // Always return the rented buffer so it is not permanently lost if TransformBlock throws.
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
