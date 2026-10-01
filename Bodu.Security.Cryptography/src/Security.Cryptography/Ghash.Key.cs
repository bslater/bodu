// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ghash.Key.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class Ghash
{
    /// <summary>
    /// Holds a hash key prepared for one function and one kernel: the key's halves for the scalar kernel, or its first
    /// four powers in the carry-less kernels' byte-reversed representation.
    /// </summary>
    /// <remarks>
    /// Every field derives from the hash key, which is secret; an owner clears its copy by assigning
    /// <see langword="default" /> to it.
    /// </remarks>
    internal readonly struct Key
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Key" /> struct from a GHASH-domain key.
        /// </summary>
        /// <param name="ghashKey">The 16-byte key, in GHASH's byte order.</param>
        /// <param name="kernel">The kernel the key is prepared for; the processor must support it.</param>
        /// <param name="isPolyval">
        /// <see langword="true" /> if the key computes POLYVAL through the GHASH isomorphism; otherwise,
        /// <see langword="false" />.
        /// </param>
        private Key(ReadOnlySpan<byte> ghashKey, KernelKind kernel, bool isPolyval)
        {
            Kernel = kernel;
            IsPolyval = isPolyval;
            High = BinaryPrimitives.ReadUInt64BigEndian(ghashKey);
            Low = BinaryPrimitives.ReadUInt64BigEndian(ghashKey[8..]);

            Vector128<byte> h1 = default;
            Vector128<byte> h2 = default;
            Vector128<byte> h3 = default;
            Vector128<byte> h4 = default;

            if (kernel == KernelKind.Pclmulqdq)
                ComputePowers<PclmulqdqIsa>(ghashKey, out h1, out h2, out h3, out h4);
            else if (kernel == KernelKind.Pmull)
                ComputePowers<PmullIsa>(ghashKey, out h1, out h2, out h3, out h4);

            H1 = h1;
            H2 = h2;
            H3 = h3;
            H4 = h4;
        }

        /// <summary>
        /// Gets the kernel the key is prepared for.
        /// </summary>
        internal KernelKind Kernel { get; }

        /// <summary>
        /// Gets a value indicating whether the key computes POLYVAL rather than GHASH.
        /// </summary>
        internal bool IsPolyval { get; }

        /// <summary>
        /// Gets the first eight bytes of the GHASH-domain key, read big-endian, for the scalar kernel.
        /// </summary>
        internal ulong High { get; }

        /// <summary>
        /// Gets the last eight bytes of the GHASH-domain key, read big-endian, for the scalar kernel.
        /// </summary>
        internal ulong Low { get; }

        /// <summary>
        /// Gets the key, <c>H</c>, byte-reversed for the carry-less kernels.
        /// </summary>
        internal Vector128<byte> H1 { get; }

        /// <summary>
        /// Gets <c>H²</c>, byte-reversed for the carry-less kernels.
        /// </summary>
        internal Vector128<byte> H2 { get; }

        /// <summary>
        /// Gets <c>H³</c>, byte-reversed for the carry-less kernels.
        /// </summary>
        internal Vector128<byte> H3 { get; }

        /// <summary>
        /// Gets <c>H⁴</c>, byte-reversed for the carry-less kernels.
        /// </summary>
        internal Vector128<byte> H4 { get; }

        /// <summary>
        /// Prepares a GHASH key.
        /// </summary>
        /// <param name="h">The 16-byte hash subkey, <c>H = E_K(0¹²⁸)</c>.</param>
        /// <param name="kernel">The kernel to prepare for; the processor must support it.</param>
        /// <returns>The prepared key.</returns>
        /// <exception cref="ArgumentException"><paramref name="h" /> is not 16 bytes.</exception>
        internal static Key ForGhash(ReadOnlySpan<byte> h, KernelKind kernel)
        {
            ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(h, BlockBytes);

            return new Key(h, kernel, isPolyval: false);
        }

        /// <summary>
        /// Prepares a POLYVAL key as the equivalent GHASH key, <c>mulX_GHASH(ByteReverse(H))</c>.
        /// </summary>
        /// <param name="h">The 16-byte POLYVAL key.</param>
        /// <param name="kernel">The kernel to prepare for; the processor must support it.</param>
        /// <returns>The prepared key.</returns>
        /// <exception cref="ArgumentException"><paramref name="h" /> is not 16 bytes.</exception>
        internal static Key ForPolyval(ReadOnlySpan<byte> h, KernelKind kernel)
        {
            ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(h, BlockBytes);

            Span<byte> ghashKey = stackalloc byte[BlockBytes];
            try
            {
                for (int i = 0; i < BlockBytes; i++)
                    ghashKey[i] = h[BlockBytes - 1 - i];

                MultiplyByX(ghashKey, ghashKey);
                return new Key(ghashKey, kernel, isPolyval: true);
            }
            finally
            {
                CryptographyHelper.Clear(ghashKey);
            }
        }
    }

    /// <summary>
    /// Multiplies a GHASH-domain element by <c>x</c> - RFC 8452 Appendix A's <c>mulX_GHASH</c>: a one-bit right shift
    /// of the block read as a big-endian integer, folding <c>0xE1</c> into its first byte when the bit shifted out was
    /// set.
    /// </summary>
    /// <param name="x">The 16-byte element.</param>
    /// <param name="result">The 16-byte destination; may be the same span as <paramref name="x" />.</param>
    /// <remarks>
    /// The reduction is applied through a mask, so the cost does not depend on the element.
    /// </remarks>
    internal static void MultiplyByX(ReadOnlySpan<byte> x, Span<byte> result)
    {
        ulong high = BinaryPrimitives.ReadUInt64BigEndian(x);
        ulong low = BinaryPrimitives.ReadUInt64BigEndian(x[8..]);

        ulong reduction = 0xE100000000000000UL & (0UL - (low & 1));
        low = (low >> 1) | (high << 63);
        high = (high >> 1) ^ reduction;

        BinaryPrimitives.WriteUInt64BigEndian(result, high);
        BinaryPrimitives.WriteUInt64BigEndian(result[8..], low);
    }
}
