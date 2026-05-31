// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpers.Overlap.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

internal static partial class CryptoHelpers
{
    /// <summary>
    /// Throws a <see cref="CryptographicException" /> if <paramref name="input" /> and <paramref name="output" />
    /// partially overlap in memory.
    /// </summary>
    /// <param name="input">The input span being read by the transform.</param>
    /// <param name="output">The output span being written by the transform.</param>
    /// <param name="allowExactInPlace">
    /// When <see langword="true" /> (the default) the spans may alias exactly — same start address and same length — so
    /// that the caller can perform an in-place transform with a single buffer. When <see langword="false" />, any
    /// overlap (including exact aliasing) is rejected.
    /// </param>
    /// <exception cref="CryptographicException">
    /// The spans overlap in a way that is not safe for forward byte-by-byte processing: either a partial overlap, or
    /// exact aliasing when <paramref name="allowExactInPlace" /> is <see langword="false" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Cryptographic transforms read input sequentially and write output sequentially. Exact in-place aliasing is safe
    /// for stream and block ciphers because each output byte is computed and written before the next input byte is
    /// read. Partial overlap, however, lets a write into not-yet-read input clobber the keystream / chaining input,
    /// producing a silently corrupted result.
    /// </para>
    /// <para>
    /// The check is short-circuited when either span is empty: zero-length operations cannot overlap meaningfully.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfInvalidOverlap(ReadOnlySpan<byte> input, Span<byte> output, bool allowExactInPlace = true)
    {
        if (input.IsEmpty || output.IsEmpty)
            return;

        if (!input.Overlaps(output, out var elementOffset))
            return;

        if (allowExactInPlace && elementOffset == 0 && input.Length == output.Length)
            return;

        throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_PartialBufferOverlap);
    }
}
