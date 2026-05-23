// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2s.Avx512.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

/// <summary>
/// AVX-512 vectorised implementation of the BLAKE2s compression function. The 16-element working vector <c>v</c> is
/// laid out as four <see cref="Vector128{T}" /> rows (<c>a, b, c, d</c>), so the four parallel <c>G</c> calls in each
/// column step collapse into a single SIMD <c>G</c> operation. The diagonal step reuses the same kernel by shifting the
/// <c>b, c, d</c> rows by 1/2/3 lanes via <c>PSHUFD</c>, running <c>G</c>, then shifting back — the canonical
/// ChaCha-style vectorisation pattern Blake2 was designed around, mirroring the BLAKE2b path with 32-bit lanes in
/// 128-bit registers.
/// </summary>
/// <remarks>
/// <para>
/// Gated on <see cref="Avx512F.VL.IsSupported" /> because the rotations use the AVX-512VL VPRORD variant on
/// <see cref="Vector128{T}" />. Every modern Intel/AMD CPU that ships AVX-512F also ships VL, so the gate is
/// effectively equivalent to "any AVX-512 host".
/// </para>
/// </remarks>
public sealed partial class Blake2s
{
    // The four rotation amounts used by Blake2s's G function (RFC 7693 §3.1). Encoded as broadcast
    // vectors so that Avx512F.VL.RotateRightVariable lowers each rotate to a single VPRORD instruction.
    private static readonly Vector128<uint> s_ror16 = Vector128.Create((uint)16);
    private static readonly Vector128<uint> s_ror12 = Vector128.Create((uint)12);
    private static readonly Vector128<uint> s_ror8 = Vector128.Create((uint)8);
    private static readonly Vector128<uint> s_ror7 = Vector128.Create((uint)7);

    /// <summary>
    /// Compresses a single 64-byte block using the AVX-512 vectorised BLAKE2s compression function.
    /// </summary>
    /// <param name="block">The 64-byte block to compress.</param>
    /// <param name="totalBytesIncludingThisBlock">The cumulative byte count including this block.</param>
    /// <param name="isFinal"><see langword="true" /> if this is the final block (inverts the f0 flag).</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void ProcessBlockAvx512(ReadOnlySpan<byte> block, ulong totalBytesIncludingThisBlock, bool isFinal)
    {
        // Load the 16 message words. Used to build the per-step (mx, my) gather vectors below.
        Span<uint> m = stackalloc uint[16];
        ref var blockRef = ref MemoryMarshal.GetReference(block);
        if (BitConverter.IsLittleEndian)
        {
            ref var wordRef = ref Unsafe.As<byte, uint>(ref blockRef);
            for (var i = 0; i < 16; i++)
                m[i] = Unsafe.Add(ref wordRef, i);
        }
        else
        {
            for (var i = 0; i < 16; i++)
                m[i] = BinaryPrimitives.ReadUInt32LittleEndian(block.Slice(i * 4, 4));
        }

        ref var mRef = ref MemoryMarshal.GetReference(m);

        // Build the four rows of the working vector. a/b come from the chaining state; c/d come from the
        // IV with the 64-bit counter XORed into lanes 0 and 1 of d (= v[12] and v[13]) and the
        // finalization flag conditionally inverting lane 2 of d (= v[14]).
        ref var hRef = ref MemoryMarshal.GetArrayDataReference(_h);
        var a = Vector128.Create(
            Unsafe.Add(ref hRef, 0),
            Unsafe.Add(ref hRef, 1),
            Unsafe.Add(ref hRef, 2),
            Unsafe.Add(ref hRef, 3));
        var b = Vector128.Create(
            Unsafe.Add(ref hRef, 4),
            Unsafe.Add(ref hRef, 5),
            Unsafe.Add(ref hRef, 6),
            Unsafe.Add(ref hRef, 7));
        var c = Vector128.Create(s_iv[0], s_iv[1], s_iv[2], s_iv[3]);
        var d = Vector128.Create(
            s_iv[4] ^ (uint)(totalBytesIncludingThisBlock & 0xFFFFFFFFUL),
            s_iv[5] ^ (uint)(totalBytesIncludingThisBlock >> 32),
            isFinal ? ~s_iv[6] : s_iv[6],
            s_iv[7]);

        // 10 rounds, each consisting of a column step followed by a diagonal step. Each step applies the
        // SIMD G kernel once across all four columns / diagonals in parallel.
        for (var r = 0; r < 10; r++)
        {
            var s = Blake2Constants.Sigma[r];

            // Column step.
            var mx = Vector128.Create(
                Unsafe.Add(ref mRef, s[0]),
                Unsafe.Add(ref mRef, s[2]),
                Unsafe.Add(ref mRef, s[4]),
                Unsafe.Add(ref mRef, s[6]));
            var my = Vector128.Create(
                Unsafe.Add(ref mRef, s[1]),
                Unsafe.Add(ref mRef, s[3]),
                Unsafe.Add(ref mRef, s[5]),
                Unsafe.Add(ref mRef, s[7]));
            GSimd(ref a, ref b, ref c, ref d, mx, my);

            // Diagonalize: shift b left by 1 lane, c left by 2, d left by 3 so diagonals become columns.
            b = Sse2.Shuffle(b.AsInt32(), 0x39).AsUInt32();  // lanes (1, 2, 3, 0)
            c = Sse2.Shuffle(c.AsInt32(), 0x4E).AsUInt32();  // lanes (2, 3, 0, 1)
            d = Sse2.Shuffle(d.AsInt32(), 0x93).AsUInt32();  // lanes (3, 0, 1, 2)

            mx = Vector128.Create(
                Unsafe.Add(ref mRef, s[8]),
                Unsafe.Add(ref mRef, s[10]),
                Unsafe.Add(ref mRef, s[12]),
                Unsafe.Add(ref mRef, s[14]));
            my = Vector128.Create(
                Unsafe.Add(ref mRef, s[9]),
                Unsafe.Add(ref mRef, s[11]),
                Unsafe.Add(ref mRef, s[13]),
                Unsafe.Add(ref mRef, s[15]));
            GSimd(ref a, ref b, ref c, ref d, mx, my);

            // Undiagonalize: invert the lane shifts to restore canonical row layout for the next round.
            b = Sse2.Shuffle(b.AsInt32(), 0x93).AsUInt32();
            c = Sse2.Shuffle(c.AsInt32(), 0x4E).AsUInt32();
            d = Sse2.Shuffle(d.AsInt32(), 0x39).AsUInt32();
        }

        // Fold the working vector back into the chaining state: h[i] ^= v[i] ^ v[i + 8].
        var hLo = Vector128.Create(
            Unsafe.Add(ref hRef, 0),
            Unsafe.Add(ref hRef, 1),
            Unsafe.Add(ref hRef, 2),
            Unsafe.Add(ref hRef, 3));
        var hHi = Vector128.Create(
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

    /// <summary>
    /// Applies the BLAKE2s <c>G</c> mixing function in parallel across four columns (or four diagonals, after the
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
        ref Vector128<uint> a, ref Vector128<uint> b, ref Vector128<uint> c, ref Vector128<uint> d,
        Vector128<uint> mx, Vector128<uint> my)
    {
        a += b + mx;
        d = Avx512F.VL.RotateRightVariable(d ^ a, s_ror16);
        c += d;
        b = Avx512F.VL.RotateRightVariable(b ^ c, s_ror12);
        a += b + my;
        d = Avx512F.VL.RotateRightVariable(d ^ a, s_ror8);
        c += d;
        b = Avx512F.VL.RotateRightVariable(b ^ c, s_ror7);
    }
}
