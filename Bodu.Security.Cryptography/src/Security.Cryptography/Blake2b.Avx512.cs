// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2b.Avx512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

/// <summary>
/// AVX-512 vectorised implementation of the BLAKE2b compression function. The 16-element working vector <c>v</c> is
/// laid out as four <see cref="Vector256{T}" /> rows (<c>a, b, c, d</c>), so the four parallel <c>G</c> calls in each
/// column step collapse into a single SIMD <c>G</c> operation. The diagonal step reuses the same kernel by shifting the
/// <c>b, c, d</c> rows by 1/2/3 lanes via <c>VPERMQ</c>, running <c>G</c>, then shifting back — the canonical
/// ChaCha-style vectorisation pattern Blake2 was designed around.
/// </summary>
/// <remarks>
/// <para>
/// Gated on <see cref="Avx512F.VL.IsSupported" /> because the rotations use the AVX-512VL VPRORQ variant on
/// <see cref="Vector256{T}" />. Every modern Intel/AMD CPU that ships AVX-512F also ships VL, so the gate is
/// effectively equivalent to "any AVX-512 host".
/// </para>
/// </remarks>
public sealed partial class Blake2b
{
    private static readonly Vector256<ulong> s_ror16 = Vector256.Create(16UL);
    private static readonly Vector256<ulong> s_ror24 = Vector256.Create(24UL);
    // The four rotation amounts used by Blake2b's G function (RFC 7693 §3.1). Encoded as broadcast
    // vectors so that Avx512F.VL.RotateRightVariable lowers each rotate to a single VPRORQ instruction.
    private static readonly Vector256<ulong> s_ror32 = Vector256.Create(32UL);
    private static readonly Vector256<ulong> s_ror63 = Vector256.Create(63UL);

    /// <summary>
    /// Applies the BLAKE2b <c>G</c> mixing function in parallel across four columns (or four diagonals, after the
    /// caller has applied the appropriate lane shifts).
    /// </summary>
    /// <param name="a">The top row of the working vector, updated in place.</param>
    /// <param name="b">The second row of the working vector, updated in place.</param>
    /// <param name="c">The third row of the working vector, updated in place.</param>
    /// <param name="d">The bottom row of the working vector, updated in place.</param>
    /// <param name="mx">The four <c>x</c> message words for this step, one per column.</param>
    /// <param name="my">The four <c>y</c> message words for this step, one per column.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GSimd(
        ref Vector256<ulong> a, ref Vector256<ulong> b, ref Vector256<ulong> c, ref Vector256<ulong> d,
        Vector256<ulong> mx, Vector256<ulong> my)
    {
        a += b + mx;
        d = Avx512F.VL.RotateRightVariable(d ^ a, s_ror32);
        c += d;
        b = Avx512F.VL.RotateRightVariable(b ^ c, s_ror24);
        a += b + my;
        d = Avx512F.VL.RotateRightVariable(d ^ a, s_ror16);
        c += d;
        b = Avx512F.VL.RotateRightVariable(b ^ c, s_ror63);
    }

    /// <summary>
    /// Compresses a single 128-byte block using the AVX-512 vectorised BLAKE2b compression function.
    /// </summary>
    /// <param name="block">The 128-byte block to compress.</param>
    /// <param name="totalBytesIncludingThisBlock">The cumulative byte count including this block.</param>
    /// <param name="isFinal"><see langword="true" /> if this is the final block (inverts the f0 flag).</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void ProcessBlockAvx512(ReadOnlySpan<byte> block, ulong totalBytesIncludingThisBlock, bool isFinal)
    {
        // Load the 16 message words. Used to build the per-step (mx, my) gather vectors below.
        Span<ulong> m = stackalloc ulong[16];
        ref byte blockRef = ref MemoryMarshal.GetReference(block);
        if (BitConverter.IsLittleEndian)
        {
            ref ulong wordRef = ref Unsafe.As<byte, ulong>(ref blockRef);
            for (int i = 0; i < 16; i++)
                m[i] = Unsafe.Add(ref wordRef, i);
        }
        else
        {
            for (int i = 0; i < 16; i++)
                m[i] = BinaryPrimitives.ReadUInt64LittleEndian(block.Slice(i * 8, 8));
        }

        ref ulong mRef = ref MemoryMarshal.GetReference(m);

        // Build the four rows of the working vector. a/b come from the chaining state; c/d come from the
        // IV with the counter XORed into lane 0 of d (= v[12]) and the finalization flag conditionally
        // inverting lane 2 of d (= v[14]).
        ref ulong hRef = ref MemoryMarshal.GetArrayDataReference(_h);
        var a = Vector256.Create(
            Unsafe.Add(ref hRef, 0),
            Unsafe.Add(ref hRef, 1),
            Unsafe.Add(ref hRef, 2),
            Unsafe.Add(ref hRef, 3));
        var b = Vector256.Create(
            Unsafe.Add(ref hRef, 4),
            Unsafe.Add(ref hRef, 5),
            Unsafe.Add(ref hRef, 6),
            Unsafe.Add(ref hRef, 7));
        var c = Vector256.Create(s_iv[0], s_iv[1], s_iv[2], s_iv[3]);
        var d = Vector256.Create(
            s_iv[4] ^ totalBytesIncludingThisBlock,
            s_iv[5],
            isFinal ? ~s_iv[6] : s_iv[6],
            s_iv[7]);

        // 12 rounds, each consisting of a column step followed by a diagonal step. Each step applies the
        // SIMD G kernel once across all four columns / diagonals in parallel.
        for (int r = 0; r < 12; r++)
        {
            byte[] s = Blake2Constants.Sigma[r % 10];

            // Column step: G applied to columns (0,4,8,12), (1,5,9,13), (2,6,10,14), (3,7,11,15) — i.e.
            // each column of the 4x4 v matrix. With a/b/c/d holding rows, lane i of each row is column i,
            // so a single SIMD G covers all four columns.
            var mx = Vector256.Create(
                Unsafe.Add(ref mRef, s[0]),
                Unsafe.Add(ref mRef, s[2]),
                Unsafe.Add(ref mRef, s[4]),
                Unsafe.Add(ref mRef, s[6]));
            var my = Vector256.Create(
                Unsafe.Add(ref mRef, s[1]),
                Unsafe.Add(ref mRef, s[3]),
                Unsafe.Add(ref mRef, s[5]),
                Unsafe.Add(ref mRef, s[7]));
            GSimd(ref a, ref b, ref c, ref d, mx, my);

            // Diagonalize: shift b left by 1 lane, c left by 2, d left by 3 so that what was the i-th
            // diagonal of the original matrix becomes the i-th column of the shuffled layout. The same
            // SIMD G then handles the four diagonals.
            b = Avx2.Permute4x64(b, 0x39);  // lanes (1, 2, 3, 0) — left rotate by 1
            c = Avx2.Permute4x64(c, 0x4E);  // lanes (2, 3, 0, 1) — left rotate by 2 (self-inverse)
            d = Avx2.Permute4x64(d, 0x93);  // lanes (3, 0, 1, 2) — left rotate by 3

            mx = Vector256.Create(
                Unsafe.Add(ref mRef, s[8]),
                Unsafe.Add(ref mRef, s[10]),
                Unsafe.Add(ref mRef, s[12]),
                Unsafe.Add(ref mRef, s[14]));
            my = Vector256.Create(
                Unsafe.Add(ref mRef, s[9]),
                Unsafe.Add(ref mRef, s[11]),
                Unsafe.Add(ref mRef, s[13]),
                Unsafe.Add(ref mRef, s[15]));
            GSimd(ref a, ref b, ref c, ref d, mx, my);

            // Undiagonalize: invert the lane shifts to restore canonical row layout for the next round.
            b = Avx2.Permute4x64(b, 0x93);  // left rotate by 3 = right rotate by 1
            c = Avx2.Permute4x64(c, 0x4E);  // self-inverse
            d = Avx2.Permute4x64(d, 0x39);  // left rotate by 1 = right rotate by 3
        }

        // Fold the working vector back into the chaining state: h[i] ^= v[i] ^ v[i + 8]. With a holding
        // v[0..3], b holding v[4..7], c holding v[8..11], and d holding v[12..15], the fold reduces to
        // h[0..3] ^= a ^ c and h[4..7] ^= b ^ d.
        var hLo = Vector256.Create(
            Unsafe.Add(ref hRef, 0),
            Unsafe.Add(ref hRef, 1),
            Unsafe.Add(ref hRef, 2),
            Unsafe.Add(ref hRef, 3));
        var hHi = Vector256.Create(
            Unsafe.Add(ref hRef, 4),
            Unsafe.Add(ref hRef, 5),
            Unsafe.Add(ref hRef, 6),
            Unsafe.Add(ref hRef, 7));
        hLo ^= a ^ c;
        hHi ^= b ^ d;

        Unsafe.Add(ref hRef, 0) = hLo.GetElement(0);
        Unsafe.Add(ref hRef, 1) = hLo.GetElement(1);
        Unsafe.Add(ref hRef, 2) = hLo.GetElement(2);
        Unsafe.Add(ref hRef, 3) = hLo.GetElement(3);
        Unsafe.Add(ref hRef, 4) = hHi.GetElement(0);
        Unsafe.Add(ref hRef, 5) = hHi.GetElement(1);
        Unsafe.Add(ref hRef, 6) = hHi.GetElement(2);
        Unsafe.Add(ref hRef, 7) = hHi.GetElement(3);
    }
}
