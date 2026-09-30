// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ghash.Clmul.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class Ghash
{
    /// <summary>
    /// Folds <paramref name="data" /> into <paramref name="state" /> with a carry-less multiply, four blocks per
    /// reduction and then a block at a time.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set that supplies the carry-less multiply.</typeparam>
    /// <param name="key">The prepared key.</param>
    /// <param name="state">The 16-byte running state, in the function's byte order; updated in place.</param>
    /// <param name="data">The data to fold in; a final partial block is padded with zeros.</param>
    /// <remarks>
    /// <para>
    /// The kernel works in GHASH's byte-reversed representation, in which a plain carry-less product, shifted left one
    /// bit and reduced, is the field product. A GHASH block and state are byte-reversed on the way in and out;
    /// POLYVAL's are already in that representation.
    /// </para>
    /// <para>
    /// Four blocks fold as <c>(Y ⊕ X₀)·H⁴ ⊕ X₁·H³ ⊕ X₂·H² ⊕ X₃·H</c>, which Horner's rule makes equal to four
    /// single-block steps. Because the shift and the reduction are linear, the four unreduced products are added first
    /// and reduced once.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    [SkipLocalsInit]
    private static void UpdateClmul<TIsa>(in Key key, Span<byte> state, ReadOnlySpan<byte> data)
        where TIsa : struct, IClmulIsa
    {
        bool reverse = !key.IsPolyval;
        Vector128<byte> h1 = key.H1;
        Vector128<byte> h2 = key.H2;
        Vector128<byte> h3 = key.H3;
        Vector128<byte> h4 = key.H4;

        Vector128<byte> y = Vector128.Create((ReadOnlySpan<byte>)state);
        if (reverse)
            y = TIsa.ReverseBytes(y);

        ref byte source = ref MemoryMarshal.GetReference(data);
        int offset = 0;

        for (; offset <= data.Length - (4 * BlockBytes); offset += 4 * BlockBytes)
        {
            Vector128<byte> x0 = LoadBlock<TIsa>(ref source, offset, reverse) ^ y;
            Vector128<byte> x1 = LoadBlock<TIsa>(ref source, offset + BlockBytes, reverse);
            Vector128<byte> x2 = LoadBlock<TIsa>(ref source, offset + (2 * BlockBytes), reverse);
            Vector128<byte> x3 = LoadBlock<TIsa>(ref source, offset + (3 * BlockBytes), reverse);

            Vector128<ulong> low = Vector128<ulong>.Zero;
            Vector128<ulong> high = Vector128<ulong>.Zero;
            Vector128<ulong> middle = Vector128<ulong>.Zero;
            Accumulate<TIsa>(x0, h4, ref low, ref high, ref middle);
            Accumulate<TIsa>(x1, h3, ref low, ref high, ref middle);
            Accumulate<TIsa>(x2, h2, ref low, ref high, ref middle);
            Accumulate<TIsa>(x3, h1, ref low, ref high, ref middle);
            y = Reduce<TIsa>(low, high, middle);
        }

        for (; offset <= data.Length - BlockBytes; offset += BlockBytes)
            y = Multiply<TIsa>(LoadBlock<TIsa>(ref source, offset, reverse) ^ y, h1);

        if (offset < data.Length)
        {
            Span<byte> last = stackalloc byte[BlockBytes];
            last.Clear();
            data[offset..].CopyTo(last);

            Vector128<byte> block = Vector128.Create((ReadOnlySpan<byte>)last);
            y = Multiply<TIsa>((reverse ? TIsa.ReverseBytes(block) : block) ^ y, h1);
            CryptographyHelper.Clear(last);
        }

        if (reverse)
            y = TIsa.ReverseBytes(y);

        y.CopyTo(state);
    }

    /// <summary>
    /// Computes <c>H</c>, <c>H²</c>, <c>H³</c>, and <c>H⁴</c> in the carry-less kernels' byte-reversed representation.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set that supplies the carry-less multiply.</typeparam>
    /// <param name="ghashKey">The 16-byte GHASH-domain key.</param>
    /// <param name="h1">Receives <c>H</c>.</param>
    /// <param name="h2">Receives <c>H²</c>.</param>
    /// <param name="h3">Receives <c>H³</c>.</param>
    /// <param name="h4">Receives <c>H⁴</c>.</param>
    private static void ComputePowers<TIsa>(
        ReadOnlySpan<byte> ghashKey,
        out Vector128<byte> h1,
        out Vector128<byte> h2,
        out Vector128<byte> h3,
        out Vector128<byte> h4)
        where TIsa : struct, IClmulIsa
    {
        h1 = TIsa.ReverseBytes(Vector128.Create(ghashKey));
        h2 = Multiply<TIsa>(h1, h1);
        h3 = Multiply<TIsa>(h2, h1);
        h4 = Multiply<TIsa>(h3, h1);
    }

    /// <summary>
    /// Loads one block, byte-reversing it into the kernels' representation when the function is GHASH.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set that supplies the byte reversal.</typeparam>
    /// <param name="source">A reference to the first byte of the data.</param>
    /// <param name="offset">The block's offset, in bytes.</param>
    /// <param name="reverse"><see langword="true" /> to byte-reverse the block.</param>
    /// <returns>The block, in the kernels' representation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> LoadBlock<TIsa>(ref byte source, int offset, bool reverse)
        where TIsa : struct, IClmulIsa
    {
        Vector128<byte> block = Vector128.LoadUnsafe(ref source, (nuint)offset);
        return reverse ? TIsa.ReverseBytes(block) : block;
    }

    /// <summary>
    /// Multiplies two elements in the kernels' byte-reversed representation.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set that supplies the carry-less multiply.</typeparam>
    /// <param name="left">The first factor.</param>
    /// <param name="right">The second factor.</param>
    /// <returns>The reduced product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> Multiply<TIsa>(Vector128<byte> left, Vector128<byte> right)
        where TIsa : struct, IClmulIsa
    {
        Vector128<ulong> low = Vector128<ulong>.Zero;
        Vector128<ulong> high = Vector128<ulong>.Zero;
        Vector128<ulong> middle = Vector128<ulong>.Zero;
        Accumulate<TIsa>(left, right, ref low, ref high, ref middle);
        return Reduce<TIsa>(low, high, middle);
    }

    /// <summary>
    /// Adds the unreduced 256-bit carry-less product of two elements into a running sum kept as its low, high, and
    /// middle 128-bit terms.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set that supplies the carry-less multiply.</typeparam>
    /// <param name="left">The first factor.</param>
    /// <param name="right">The second factor.</param>
    /// <param name="low">The sum of the products of the low halves; updated in place.</param>
    /// <param name="high">The sum of the products of the high halves; updated in place.</param>
    /// <param name="middle">The sum of the two cross products; updated in place.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Accumulate<TIsa>(
        Vector128<byte> left,
        Vector128<byte> right,
        ref Vector128<ulong> low,
        ref Vector128<ulong> high,
        ref Vector128<ulong> middle)
        where TIsa : struct, IClmulIsa
    {
        Vector128<ulong> a = left.AsUInt64();
        Vector128<ulong> b = right.AsUInt64();

        low ^= TIsa.MultiplyLower(a, b);
        high ^= TIsa.MultiplyUpper(a, b);
        middle ^= TIsa.MultiplyLowerUpper(a, b) ^ TIsa.MultiplyUpperLower(a, b);
    }

    /// <summary>
    /// Reduces a 256-bit carry-less product, given as its low, high, and middle terms, to a field element in the
    /// kernels' byte-reversed representation - the Gueron-Kounavis method: fold the middle term into the halves, shift
    /// the whole product left one bit, then reduce modulo <c>x¹²⁸ + x⁷ + x² + x + 1</c> in two phases.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set that supplies the byte shifts.</typeparam>
    /// <param name="low">The product's low term.</param>
    /// <param name="high">The product's high term.</param>
    /// <param name="middle">The product's middle term.</param>
    /// <returns>The reduced element.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> Reduce<TIsa>(Vector128<ulong> low, Vector128<ulong> high, Vector128<ulong> middle)
        where TIsa : struct, IClmulIsa
    {
        Vector128<uint> lowWords = (low ^ TIsa.ShiftBytesLeft8(middle.AsByte()).AsUInt64()).AsUInt32();
        Vector128<uint> highWords = (high ^ TIsa.ShiftBytesRight8(middle.AsByte()).AsUInt64()).AsUInt32();

        // Shift the 256-bit product left one bit, to compensate for the reflected bit order.
        Vector128<uint> lowCarry = Vector128.ShiftRightLogical(lowWords, 31);
        Vector128<uint> highCarry = Vector128.ShiftRightLogical(highWords, 31);
        lowWords = Vector128.ShiftLeft(lowWords, 1);
        highWords = Vector128.ShiftLeft(highWords, 1);
        Vector128<uint> crossCarry = TIsa.ShiftBytesRight12(lowCarry.AsByte()).AsUInt32();
        highCarry = TIsa.ShiftBytesLeft4(highCarry.AsByte()).AsUInt32();
        lowCarry = TIsa.ShiftBytesLeft4(lowCarry.AsByte()).AsUInt32();
        lowWords |= lowCarry;
        highWords |= highCarry | crossCarry;

        // First reduction phase: fold the terms the polynomial's x⁷, x², and x¹ produce from the low half.
        Vector128<uint> fold = Vector128.ShiftLeft(lowWords, 31) ^ Vector128.ShiftLeft(lowWords, 30) ^ Vector128.ShiftLeft(lowWords, 25);
        Vector128<uint> spill = TIsa.ShiftBytesRight4(fold.AsByte()).AsUInt32();
        lowWords ^= TIsa.ShiftBytesLeft12(fold.AsByte()).AsUInt32();

        // Second reduction phase: complete the fold into the high half.
        Vector128<uint> rest = Vector128.ShiftRightLogical(lowWords, 1) ^ Vector128.ShiftRightLogical(lowWords, 2) ^ Vector128.ShiftRightLogical(lowWords, 7);
        lowWords ^= rest ^ spill;

        return (highWords ^ lowWords).AsByte();
    }
}
