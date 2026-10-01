// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeccakPermutation.Vector256.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

internal static partial class KeccakPermutation
{
    /// <summary>
    /// Gets a value indicating whether a vector kernel runs <see cref="Permute4(Span{Vector256{ulong}})" />, so that
    /// four sponges advanced together cost less than four advanced one at a time.
    /// </summary>
    internal static bool IsFourWayAccelerated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => SimdCapabilities.Avx2;
    }

    /// <summary>
    /// Applies the <c>Keccak-f[1600]</c> permutation to four states at once with the kernel dispatch selects.
    /// </summary>
    /// <param name="states">
    /// The four states, interleaved: vector <c>i</c> holds lane <c>i</c> of the four states, state <c>j</c> in its
    /// element <c>j</c>. Modified in place.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="states" /> is not exactly 25 vectors long.</exception>
    internal static void Permute4(Span<Vector256<ulong>> states) =>
        Permute4(KernelKind.Auto, states);

    /// <summary>
    /// Applies the <c>Keccak-f[1600]</c> permutation to four states at once with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="states">
    /// The four states, interleaved: vector <c>i</c> holds lane <c>i</c> of the four states, state <c>j</c> in its
    /// element <c>j</c>. Modified in place.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="states" /> is not exactly 25 vectors long.</exception>
    internal static void Permute4(KernelKind kernel, Span<Vector256<ulong>> states)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(states, StateWords);

        ref Vector256<ulong> interleaved = ref MemoryMarshal.GetReference(states);
        switch (kernel == KernelKind.Auto ? SelectKernel() : kernel)
        {
            case KernelKind.Avx512:
                Vector256Kernel<VectorRotation.Avx512>.Permute(ref interleaved);
                break;

            case KernelKind.Avx2:
                Vector256Kernel<VectorRotation.Avx2>.Permute(ref interleaved);
                break;

            default:
                Permute4Scalar(ref interleaved);
                break;
        }
    }

    /// <summary>
    /// Selects the kernel for the four-way permutation: AVX-512VL's, then AVX2's, then four scalar permutations.
    /// </summary>
    /// <returns>The kernel dispatch runs; never <see cref="KernelKind.Auto" />.</returns>
    /// <remarks>
    /// Every gate honors the <see cref="SimdCapabilities.DisableSimdSwitchName" /> switch, which pins the scalar
    /// kernel.
    /// </remarks>
    internal static KernelKind SelectKernel()
    {
        if (SimdCapabilities.Avx512FVL)
            return KernelKind.Avx512;

        return SimdCapabilities.Avx2 ? KernelKind.Avx2 : KernelKind.Scalar;
    }

    /// <summary>
    /// Determines whether the processor can run the specified kernel, whether or not the process allows vector code.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <returns>
    /// <see langword="true" /> if the processor supports every instruction the kernel uses; otherwise,
    /// <see langword="false" />.
    /// </returns>
    internal static bool IsSupported(KernelKind kernel) => kernel switch
    {
        KernelKind.Auto or KernelKind.Scalar => true,
        KernelKind.Avx2 => System.Runtime.Intrinsics.X86.Avx2.IsSupported,
        KernelKind.Avx512 => System.Runtime.Intrinsics.X86.Avx2.IsSupported && System.Runtime.Intrinsics.X86.Avx512F.VL.IsSupported,
        _ => false,
    };

    /// <summary>
    /// Applies the scalar permutation to each of four interleaved states in turn.
    /// </summary>
    /// <param name="states">The first of the 25 interleaved vectors. Modified in place.</param>
    private static void Permute4Scalar(ref Vector256<ulong> states)
    {
        Span<ulong> state = stackalloc ulong[StateWords];
        for (int lane = 0; lane < 4; lane++)
        {
            for (int i = 0; i < StateWords; i++)
                state[i] = Unsafe.Add(ref states, i).GetElement(lane);

            Permute(state);

            for (int i = 0; i < StateWords; i++)
                Unsafe.Add(ref states, i) = Unsafe.Add(ref states, i).WithElement(lane, state[i]);
        }

        CryptographyHelper.Clear(state);
    }

    /// <summary>
    /// Provides the four-way <c>Keccak-f[1600]</c> permutation over 256-bit vectors, one state per 64-bit lane,
    /// rotating through the instructions of <typeparamref name="TIsa" />.
    /// </summary>
    /// <typeparam name="TIsa">
    /// The instruction set that rotates each lane: <see cref="VectorRotation.Avx2" /> or
    /// <see cref="VectorRotation.Avx512" />.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// The steps are those of <see cref="KeccakPermutation.Permute(Span{ulong})" />, each applied to four lanes at
    /// once. Over <see cref="VectorRotation.Avx512" /> every rotation is one instruction, and each column parity and
    /// each χ expression is written as AVX-512VL three-input logic: two instructions for a parity, one for
    /// <c>a ^ (~b &amp; c)</c>. Left to fold the plain expressions itself, the JIT used more instructions and more
    /// registers, and the state no longer fit in them.
    /// </para>
    /// <para>
    /// No lane is ever used as an index or a branch condition, so the permutation takes the same time and touches the
    /// same memory whatever the states hold.
    /// </para>
    /// </remarks>
    internal static class Vector256Kernel<TIsa>
        where TIsa : struct, IVector256Rotation
    {
        /// <summary>
        /// Applies the full permutation, 24 rounds, to four interleaved states in place.
        /// </summary>
        /// <param name="states">The first of the 25 interleaved vectors. Modified in place.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void Permute(ref Vector256<ulong> states)
        {
            Vector256<ulong> a00 = Unsafe.Add(ref states, 0);
            Vector256<ulong> a01 = Unsafe.Add(ref states, 1);
            Vector256<ulong> a02 = Unsafe.Add(ref states, 2);
            Vector256<ulong> a03 = Unsafe.Add(ref states, 3);
            Vector256<ulong> a04 = Unsafe.Add(ref states, 4);
            Vector256<ulong> a05 = Unsafe.Add(ref states, 5);
            Vector256<ulong> a06 = Unsafe.Add(ref states, 6);
            Vector256<ulong> a07 = Unsafe.Add(ref states, 7);
            Vector256<ulong> a08 = Unsafe.Add(ref states, 8);
            Vector256<ulong> a09 = Unsafe.Add(ref states, 9);
            Vector256<ulong> a10 = Unsafe.Add(ref states, 10);
            Vector256<ulong> a11 = Unsafe.Add(ref states, 11);
            Vector256<ulong> a12 = Unsafe.Add(ref states, 12);
            Vector256<ulong> a13 = Unsafe.Add(ref states, 13);
            Vector256<ulong> a14 = Unsafe.Add(ref states, 14);
            Vector256<ulong> a15 = Unsafe.Add(ref states, 15);
            Vector256<ulong> a16 = Unsafe.Add(ref states, 16);
            Vector256<ulong> a17 = Unsafe.Add(ref states, 17);
            Vector256<ulong> a18 = Unsafe.Add(ref states, 18);
            Vector256<ulong> a19 = Unsafe.Add(ref states, 19);
            Vector256<ulong> a20 = Unsafe.Add(ref states, 20);
            Vector256<ulong> a21 = Unsafe.Add(ref states, 21);
            Vector256<ulong> a22 = Unsafe.Add(ref states, 22);
            Vector256<ulong> a23 = Unsafe.Add(ref states, 23);
            Vector256<ulong> a24 = Unsafe.Add(ref states, 24);

            ref ulong roundConstants = ref MemoryMarshal.GetReference(RoundConstants);
            for (int round = 0; round < 24; round++)
            {
                // θ: the parity of each column, and the offset each column receives from its neighbors.
                Vector256<ulong> c0 = Parity(a00, a05, a10, a15, a20);
                Vector256<ulong> c1 = Parity(a01, a06, a11, a16, a21);
                Vector256<ulong> c2 = Parity(a02, a07, a12, a17, a22);
                Vector256<ulong> c3 = Parity(a03, a08, a13, a18, a23);
                Vector256<ulong> c4 = Parity(a04, a09, a14, a19, a24);
                Vector256<ulong> d0 = c4 ^ c1.RotateBitsLeftUnchecked<TIsa>(1);
                Vector256<ulong> d1 = c0 ^ c2.RotateBitsLeftUnchecked<TIsa>(1);
                Vector256<ulong> d2 = c1 ^ c3.RotateBitsLeftUnchecked<TIsa>(1);
                Vector256<ulong> d3 = c2 ^ c4.RotateBitsLeftUnchecked<TIsa>(1);
                Vector256<ulong> d4 = c3 ^ c0.RotateBitsLeftUnchecked<TIsa>(1);

                // ρ and π: lane (x, y), θ-adjusted and rotated by its offset, moves to position (y, 2x + 3y).
                Vector256<ulong> b00 = a00 ^ d0;
                Vector256<ulong> b01 = (a06 ^ d1).RotateBitsLeftUnchecked<TIsa>(44);
                Vector256<ulong> b02 = (a12 ^ d2).RotateBitsLeftUnchecked<TIsa>(43);
                Vector256<ulong> b03 = (a18 ^ d3).RotateBitsLeftUnchecked<TIsa>(21);
                Vector256<ulong> b04 = (a24 ^ d4).RotateBitsLeftUnchecked<TIsa>(14);
                Vector256<ulong> b05 = (a03 ^ d3).RotateBitsLeftUnchecked<TIsa>(28);
                Vector256<ulong> b06 = (a09 ^ d4).RotateBitsLeftUnchecked<TIsa>(20);
                Vector256<ulong> b07 = (a10 ^ d0).RotateBitsLeftUnchecked<TIsa>(3);
                Vector256<ulong> b08 = (a16 ^ d1).RotateBitsLeftUnchecked<TIsa>(45);
                Vector256<ulong> b09 = (a22 ^ d2).RotateBitsLeftUnchecked<TIsa>(61);
                Vector256<ulong> b10 = (a01 ^ d1).RotateBitsLeftUnchecked<TIsa>(1);
                Vector256<ulong> b11 = (a07 ^ d2).RotateBitsLeftUnchecked<TIsa>(6);
                Vector256<ulong> b12 = (a13 ^ d3).RotateBitsLeftUnchecked<TIsa>(25);
                Vector256<ulong> b13 = (a19 ^ d4).RotateBitsLeftUnchecked<TIsa>(8);
                Vector256<ulong> b14 = (a20 ^ d0).RotateBitsLeftUnchecked<TIsa>(18);
                Vector256<ulong> b15 = (a04 ^ d4).RotateBitsLeftUnchecked<TIsa>(27);
                Vector256<ulong> b16 = (a05 ^ d0).RotateBitsLeftUnchecked<TIsa>(36);
                Vector256<ulong> b17 = (a11 ^ d1).RotateBitsLeftUnchecked<TIsa>(10);
                Vector256<ulong> b18 = (a17 ^ d2).RotateBitsLeftUnchecked<TIsa>(15);
                Vector256<ulong> b19 = (a23 ^ d3).RotateBitsLeftUnchecked<TIsa>(56);
                Vector256<ulong> b20 = (a02 ^ d2).RotateBitsLeftUnchecked<TIsa>(62);
                Vector256<ulong> b21 = (a08 ^ d3).RotateBitsLeftUnchecked<TIsa>(55);
                Vector256<ulong> b22 = (a14 ^ d4).RotateBitsLeftUnchecked<TIsa>(39);
                Vector256<ulong> b23 = (a15 ^ d0).RotateBitsLeftUnchecked<TIsa>(41);
                Vector256<ulong> b24 = (a21 ^ d1).RotateBitsLeftUnchecked<TIsa>(2);

                // χ and ι: each output lane mixes the next two in its row; the round constant enters lane (0, 0).
                a00 = Chi(b00, b01, b02) ^ Vector256.Create(Unsafe.Add(ref roundConstants, round));
                a01 = Chi(b01, b02, b03);
                a02 = Chi(b02, b03, b04);
                a03 = Chi(b03, b04, b00);
                a04 = Chi(b04, b00, b01);
                a05 = Chi(b05, b06, b07);
                a06 = Chi(b06, b07, b08);
                a07 = Chi(b07, b08, b09);
                a08 = Chi(b08, b09, b05);
                a09 = Chi(b09, b05, b06);
                a10 = Chi(b10, b11, b12);
                a11 = Chi(b11, b12, b13);
                a12 = Chi(b12, b13, b14);
                a13 = Chi(b13, b14, b10);
                a14 = Chi(b14, b10, b11);
                a15 = Chi(b15, b16, b17);
                a16 = Chi(b16, b17, b18);
                a17 = Chi(b17, b18, b19);
                a18 = Chi(b18, b19, b15);
                a19 = Chi(b19, b15, b16);
                a20 = Chi(b20, b21, b22);
                a21 = Chi(b21, b22, b23);
                a22 = Chi(b22, b23, b24);
                a23 = Chi(b23, b24, b20);
                a24 = Chi(b24, b20, b21);
            }

            Unsafe.Add(ref states, 0) = a00;
            Unsafe.Add(ref states, 1) = a01;
            Unsafe.Add(ref states, 2) = a02;
            Unsafe.Add(ref states, 3) = a03;
            Unsafe.Add(ref states, 4) = a04;
            Unsafe.Add(ref states, 5) = a05;
            Unsafe.Add(ref states, 6) = a06;
            Unsafe.Add(ref states, 7) = a07;
            Unsafe.Add(ref states, 8) = a08;
            Unsafe.Add(ref states, 9) = a09;
            Unsafe.Add(ref states, 10) = a10;
            Unsafe.Add(ref states, 11) = a11;
            Unsafe.Add(ref states, 12) = a12;
            Unsafe.Add(ref states, 13) = a13;
            Unsafe.Add(ref states, 14) = a14;
            Unsafe.Add(ref states, 15) = a15;
            Unsafe.Add(ref states, 16) = a16;
            Unsafe.Add(ref states, 17) = a17;
            Unsafe.Add(ref states, 18) = a18;
            Unsafe.Add(ref states, 19) = a19;
            Unsafe.Add(ref states, 20) = a20;
            Unsafe.Add(ref states, 21) = a21;
            Unsafe.Add(ref states, 22) = a22;
            Unsafe.Add(ref states, 23) = a23;
            Unsafe.Add(ref states, 24) = a24;
        }

        /// <summary>
        /// Returns the exclusive-or of five vectors: one column's parity.
        /// </summary>
        /// <param name="a">The first lane of the column.</param>
        /// <param name="b">The second lane of the column.</param>
        /// <param name="c">The third lane of the column.</param>
        /// <param name="d">The fourth lane of the column.</param>
        /// <param name="e">The fifth lane of the column.</param>
        /// <returns>The parity, lane by lane.</returns>
        /// <remarks>
        /// The choice is an <see langword="if" /> rather than a conditional expression: .NET 10 gives each inlined
        /// conditional expression a temporary of its own, and with one per parity and per χ step the state no longer
        /// fit in the registers.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> Parity(Vector256<ulong> a, Vector256<ulong> b, Vector256<ulong> c, Vector256<ulong> d, Vector256<ulong> e)
        {
            if (typeof(TIsa) == typeof(VectorRotation.Avx512))
                return Avx512F.VL.TernaryLogic(Avx512F.VL.TernaryLogic(a, b, c, 0x96), d, e, 0x96);

            return a ^ b ^ c ^ d ^ e;
        }

        /// <summary>
        /// Returns the χ step's <c>a ^ (~b &amp; c)</c>.
        /// </summary>
        /// <param name="a">The lane that is updated.</param>
        /// <param name="b">The next lane in its row.</param>
        /// <param name="c">The lane after that.</param>
        /// <returns>The updated lane.</returns>
        /// <remarks>
        /// The choice is an <see langword="if" /> rather than a conditional expression, for the reason
        /// <see cref="Parity" /> gives.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> Chi(Vector256<ulong> a, Vector256<ulong> b, Vector256<ulong> c)
        {
            if (typeof(TIsa) == typeof(VectorRotation.Avx512))
                return Avx512F.VL.TernaryLogic(a, b, c, 0xD2);

            return a ^ (~b & c);
        }
    }
}
