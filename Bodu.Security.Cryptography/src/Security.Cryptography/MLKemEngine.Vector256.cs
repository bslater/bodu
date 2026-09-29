// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngine.Vector256.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class MLKemEngine
{
    /// <summary>
    /// Provides the ML-KEM transforms and base-case products over AVX2, sixteen coefficients per 256-bit vector,
    /// computing exactly the values the scalar code computes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every value the scalar code forms fits in 16 bits: its transforms keep each coefficient below 8q = 26632 in
    /// magnitude, and each reduction's result is below q. So the kernel works on a 16-bit copy of the int coefficients,
    /// packed when they are loaded and sign-extended when they are stored, and computes each step as the reference
    /// implementation's AVX2 code does, with the 16-bit multiplications <c>VPMULLW</c> and <c>VPMULHW</c>.
    /// </para>
    /// <para>
    /// A Montgomery product takes the low half of a·(ζ·q⁻¹) as the multiple m, then subtracts the high half of m·q from
    /// the high half of a·ζ. The low halves of a·ζ and m·q are equal by the choice of m, so the difference is exactly
    /// <see cref="MontgomeryReduce" /> of the product. A Barrett reduction's quotient, the high half of 20159·a rounded
    /// by 2^9 and shifted by 10 bits, is exactly the quotient <see cref="BarrettReduce" /> forms.
    /// </para>
    /// <para>
    /// Each transform makes one pass over the 16-bit copy for the four layers whose partners lie a whole vector or more
    /// apart, and one over each block of 32 coefficients for the other three, after an in-register rearrangement that
    /// gives each pair of partners one lane. No coefficient is ever used as an index or a branch condition.
    /// </para>
    /// </remarks>
    internal static class Vector256Kernel
    {
        /// <summary>The number of coefficients in each vector.</summary>
        private const int Lanes = 16;

        /// <summary>The blend mask that takes the odd 32-bit lanes from the second operand.</summary>
        private const byte OddLanes = 0b1010_1010;

        /// <summary>The number of shorts each layer's lane-ordered twiddles take for one block: two vectors.</summary>
        private const int LayerTwiddleShorts = 2 * Lanes;

        /// <summary>The multiplier ⌊(2^26 + ⌊q / 2⌋) / q⌋ of <see cref="BarrettReduce" />, which fits in 16 bits.</summary>
        private const short BarrettMultiplier16 = (short)BarrettMultiplier;

        /// <summary>The twiddle table, each entry narrowed to 16 bits.</summary>
        private static readonly short[] s_zetas16 = BuildZetas16(timesQInverse: false);

        /// <summary>Each twiddle times q⁻¹ mod 2^16, the second factor of each broadcast product.</summary>
        private static readonly short[] s_zetasQInverse16 = BuildZetas16(timesQInverse: true);

        /// <summary>The forward transform's twiddles for its last three layers, lane by lane.</summary>
        private static readonly short[] s_forwardTwiddles = BuildLaneTwiddles(forward: true);

        /// <summary>The inverse transform's twiddles for its first three layers, lane by lane.</summary>
        private static readonly short[] s_inverseTwiddles = BuildLaneTwiddles(forward: false);

        /// <summary>For each pair of the base-case products, γ · 2^16 mod q and its multiple of q⁻¹, sixteen pairs per vector.</summary>
        private static readonly short[] s_gammas16 = BuildGammas16();

        /// <summary>
        /// Applies the forward NTT to 256 coefficients in place, as
        /// <see cref="MLKemEngine.Ntt(KernelKind, Span{int})" /> documents.
        /// </summary>
        /// <param name="coefficients">The first of the 256 coefficients.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void Ntt(ref int coefficients)
        {
            Span<short> copy = stackalloc short[N];
            ref short work = ref MemoryMarshal.GetReference(copy);
            for (nuint j = 0; j < N; j += Lanes)
                Pack(ref coefficients, j).StoreUnsafe(ref work, j);

            // Layers of 128, 64, 32 and 16: partners a whole vector or more apart, one twiddle per block.
            ref short zetas = ref MemoryMarshal.GetArrayDataReference(s_zetas16);
            ref short zetasQ = ref MemoryMarshal.GetArrayDataReference(s_zetasQInverse16);
            int m = 0;
            for (int length = 128; length >= Lanes; length >>= 1)
            {
                for (int start = 0; start < N; start += 2 * length)
                {
                    ++m;
                    Vector256<short> zeta = Vector256.Create(Unsafe.Add(ref zetas, m));
                    Vector256<short> zetaQ = Vector256.Create(Unsafe.Add(ref zetasQ, m));
                    for (int j = start; j < start + length; j += Lanes)
                    {
                        Vector256<short> low = Vector256.LoadUnsafe(ref work, (nuint)j);
                        Vector256<short> high = Vector256.LoadUnsafe(ref work, (nuint)(j + length));
                        Vector256<short> t = MultiplyTwiddles(high, zeta, zetaQ);
                        Avx2.Subtract(low, t).StoreUnsafe(ref work, (nuint)(j + length));
                        Avx2.Add(low, t).StoreUnsafe(ref work, (nuint)j);
                    }
                }
            }

            // Layers of 8, 4 and 2 on each block of 32 coefficients, reduced and widened as they are stored.
            ref short twiddles = ref MemoryMarshal.GetArrayDataReference(s_forwardTwiddles);
            for (int block = 0; block < 8; block++)
            {
                nuint offset = (nuint)(32 * block);
                Vector256<short> first = Vector256.LoadUnsafe(ref work, offset);
                Vector256<short> second = Vector256.LoadUnsafe(ref work, offset + Lanes);
                LastLayers(ref first, ref second, ref Unsafe.Add(ref twiddles, block * 3 * LayerTwiddleShorts));

                Unpack(Canonicalize(Barrett(first)), ref coefficients, offset);
                Unpack(Canonicalize(Barrett(second)), ref coefficients, offset + Lanes);
            }

            CryptographyHelper.Clear(copy);
        }

        /// <summary>
        /// Applies the inverse NTT to 256 coefficients in place, including the final scaling by 128⁻¹, as
        /// <see cref="MLKemEngine.InvNtt(KernelKind, Span{int})" /> documents.
        /// </summary>
        /// <param name="coefficients">The first of the 256 coefficients.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void InvNtt(ref int coefficients)
        {
            Span<short> copy = stackalloc short[N];
            ref short work = ref MemoryMarshal.GetReference(copy);

            // Layers of 2, 4 and 8 on each block of 32 coefficients, packed as they are loaded.
            ref short twiddles = ref MemoryMarshal.GetArrayDataReference(s_inverseTwiddles);
            for (int block = 0; block < 8; block++)
            {
                nuint offset = (nuint)(32 * block);
                Vector256<short> first = Pack(ref coefficients, offset);
                Vector256<short> second = Pack(ref coefficients, offset + Lanes);
                FirstInverseLayers(ref first, ref second, ref Unsafe.Add(ref twiddles, block * 3 * LayerTwiddleShorts));

                first.StoreUnsafe(ref work, offset);
                second.StoreUnsafe(ref work, offset + Lanes);
            }

            // Layers of 16, 32, 64 and 128.
            ref short zetas = ref MemoryMarshal.GetArrayDataReference(s_zetas16);
            ref short zetasQ = ref MemoryMarshal.GetArrayDataReference(s_zetasQInverse16);
            int m = 16;
            for (int length = Lanes; length <= 128; length <<= 1)
            {
                for (int start = 0; start < N; start += 2 * length)
                {
                    --m;
                    Vector256<short> zeta = Vector256.Create(Unsafe.Add(ref zetas, m));
                    Vector256<short> zetaQ = Vector256.Create(Unsafe.Add(ref zetasQ, m));
                    for (int j = start; j < start + length; j += Lanes)
                    {
                        Vector256<short> t = Vector256.LoadUnsafe(ref work, (nuint)j);
                        Vector256<short> u = Vector256.LoadUnsafe(ref work, (nuint)(j + length));
                        Barrett(Avx2.Add(t, u)).StoreUnsafe(ref work, (nuint)j);
                        MultiplyTwiddles(Avx2.Subtract(u, t), zeta, zetaQ).StoreUnsafe(ref work, (nuint)(j + length));
                    }
                }
            }

            // The scaling by 128⁻¹, in Montgomery form, widened as it is stored.
            Vector256<short> scale = Vector256.Create((short)InverseOf128Montgomery);
            Vector256<short> scaleQ = Vector256.Create(unchecked((short)(InverseOf128Montgomery * QInverse)));
            for (nuint j = 0; j < N; j += Lanes)
                Unpack(Canonicalize(MultiplyTwiddles(Vector256.LoadUnsafe(ref work, j), scale, scaleQ)), ref coefficients, j);

            CryptographyHelper.Clear(copy);
        }

        /// <summary>
        /// Multiplies two NTT-domain polynomials with the base-case products, as
        /// <see cref="MLKemEngine.MultiplyNtt(KernelKind, ReadOnlySpan{int}, ReadOnlySpan{int}, Span{int})" />
        /// documents.
        /// </summary>
        /// <param name="left">The first of the 256 coefficients of the first factor, each in [0, q).</param>
        /// <param name="right">The first of the 256 coefficients of the second factor, each in [0, q).</param>
        /// <param name="destination">The first of the 256 coefficients receiving the product, each in [0, q).</param>
        /// <remarks>
        /// Sixteen pairs at a time, the evens and the odds of each factor split into separate vectors. With plain
        /// factors, each Montgomery product carries 2^−16: r₀·2^−16 = a₀b₀·2^−16 + (a₁b₁·2^−16)·(γ·2^16)·2^−16 and
        /// r₁·2^−16 = a₀b₁·2^−16 + a₁b₀·2^−16, each sum below 2q in magnitude, and one more product by 2^32 mod q
        /// removes the factor. The results are canonical, so they are the values <see cref="ReduceWide" /> returns.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void MultiplyNtt(ref int left, ref int right, ref int destination)
        {
            ref short gammas = ref MemoryMarshal.GetArrayDataReference(s_gammas16);
            Vector256<short> montgomerySquared = Vector256.Create((short)MontgomerySquared);
            Vector256<short> montgomerySquaredQ = Vector256.Create(unchecked((short)(MontgomerySquared * QInverse)));

            for (int group = 0; group < 8; group++)
            {
                nuint offset = (nuint)(32 * group);
                SplitPairs(ref left, offset, out Vector256<short> a0, out Vector256<short> a1);
                SplitPairs(ref right, offset, out Vector256<short> b0, out Vector256<short> b1);

                ref short groupGammas = ref Unsafe.Add(ref gammas, group * 2 * Lanes);
                Vector256<short> oddProduct = MultiplyReduce(a1, b1);
                Vector256<short> evenSum = Avx2.Add(
                    MultiplyReduce(a0, b0),
                    MultiplyTwiddles(oddProduct, Vector256.LoadUnsafe(ref groupGammas), Vector256.LoadUnsafe(ref groupGammas, Lanes)));
                Vector256<short> oddSum = Avx2.Add(MultiplyReduce(a0, b1), MultiplyReduce(a1, b0));

                Vector256<short> even = Canonicalize(MultiplyTwiddles(evenSum, montgomerySquared, montgomerySquaredQ));
                Vector256<short> odd = Canonicalize(MultiplyTwiddles(oddSum, montgomerySquared, montgomerySquaredQ));

                // Interleave back into pairs: within each 128-bit half, pairs 0–3 then 4–7 of that half's eight.
                Vector256<short> low = Avx2.UnpackLow(even, odd);
                Vector256<short> high = Avx2.UnpackHigh(even, odd);
                Unpack(Avx2.Permute2x128(low, high, 0x20), ref destination, offset);
                Unpack(Avx2.Permute2x128(low, high, 0x31), ref destination, offset + Lanes);
            }
        }

        /// <summary>
        /// Returns <see cref="MontgomeryReduce" /> of the product of each lane of <paramref name="value" /> with the
        /// same lane of <paramref name="zetas" />.
        /// </summary>
        /// <param name="value">The coefficients.</param>
        /// <param name="zetas">The twiddles.</param>
        /// <param name="zetasQ">The twiddles times q⁻¹ mod 2^16.</param>
        /// <returns>The reduced products, each in (−q, q).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<short> MultiplyTwiddles(Vector256<short> value, Vector256<short> zetas, Vector256<short> zetasQ)
        {
            Vector256<short> multiple = Avx2.MultiplyLow(value, zetasQ);
            return Avx2.Subtract(Avx2.MultiplyHigh(value, zetas), Avx2.MultiplyHigh(multiple, Vector256.Create((short)Q)));
        }

        /// <summary>
        /// Returns <see cref="MontgomeryReduce" /> of the product of each lane of <paramref name="left" /> with the
        /// same lane of <paramref name="right" />, neither factor precomputed.
        /// </summary>
        /// <param name="left">The first factors.</param>
        /// <param name="right">The second factors.</param>
        /// <returns>The reduced products, each in (−q, q).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<short> MultiplyReduce(Vector256<short> left, Vector256<short> right)
        {
            Vector256<short> multiple = Avx2.MultiplyLow(Avx2.MultiplyLow(left, right), Vector256.Create((short)QInverse));
            return Avx2.Subtract(Avx2.MultiplyHigh(left, right), Avx2.MultiplyHigh(multiple, Vector256.Create((short)Q)));
        }

        /// <summary>
        /// Returns <see cref="BarrettReduce" /> of each lane: its centered representative modulo q.
        /// </summary>
        /// <param name="value">The coefficients, each below 2^15 in magnitude.</param>
        /// <returns>The representatives, each in [−(q − 1) / 2, (q − 1) / 2].</returns>
        /// <remarks>
        /// The high half of 20159·a is ⌊20159·a / 2^16⌋; adding 2^9 and shifting right 10 bits gives ⌊(20159·a + 2^25)
        /// / 2^26⌋, the scalar reduction's quotient.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<short> Barrett(Vector256<short> value)
        {
            Vector256<short> estimate = Avx2.MultiplyHigh(value, Vector256.Create(BarrettMultiplier16));
            Vector256<short> quotient = Avx2.ShiftRightArithmetic(Avx2.Add(estimate, Vector256.Create((short)(1 << 9))), 10);
            return Avx2.Subtract(value, Avx2.MultiplyLow(quotient, Vector256.Create((short)Q)));
        }

        /// <summary>
        /// Returns <see cref="MLKemEngine.Canonicalize" /> of each lane: a value in [−q, q) mapped to [0, q).
        /// </summary>
        /// <param name="value">The coefficients, each at least −q and below q.</param>
        /// <returns>The canonical representatives.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<short> Canonicalize(Vector256<short> value) =>
            Avx2.Add(value, Avx2.And(Avx2.ShiftRightArithmetic(value, 15), Vector256.Create((short)Q)));

        /// <summary>
        /// Applies one forward butterfly to two vectors of partners, each lane with its own twiddle.
        /// </summary>
        /// <param name="low">The low partners; receives their sums.</param>
        /// <param name="high">The high partners; receives their differences.</param>
        /// <param name="twiddles">The layer's two vectors of lane-ordered twiddles for the block.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LaneButterfly(ref Vector256<short> low, ref Vector256<short> high, ref short twiddles)
        {
            Vector256<short> t = MultiplyTwiddles(high, Vector256.LoadUnsafe(ref twiddles), Vector256.LoadUnsafe(ref twiddles, Lanes));
            high = Avx2.Subtract(low, t);
            low = Avx2.Add(low, t);
        }

        /// <summary>
        /// Applies one inverse butterfly to two vectors of partners, each lane with its own twiddle.
        /// </summary>
        /// <param name="low">The low partners; receives their reduced sums.</param>
        /// <param name="high">The high partners; receives their reduced, twiddled differences.</param>
        /// <param name="twiddles">The layer's two vectors of lane-ordered twiddles for the block.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InverseLaneButterfly(ref Vector256<short> low, ref Vector256<short> high, ref short twiddles)
        {
            Vector256<short> t = low;
            low = Barrett(Avx2.Add(t, high));
            high = MultiplyTwiddles(Avx2.Subtract(high, t), Vector256.LoadUnsafe(ref twiddles), Vector256.LoadUnsafe(ref twiddles, Lanes));
        }

        /// <summary>
        /// Applies the forward transform's layers of 8, 4 and 2 to 32 consecutive coefficients.
        /// </summary>
        /// <param name="first">The first sixteen coefficients; receives them transformed.</param>
        /// <param name="second">The next sixteen; receives them transformed.</param>
        /// <param name="twiddles">The block's lane-ordered twiddles for the three layers, in that order.</param>
        /// <remarks>
        /// Before each layer the coefficients are rearranged so that each pair of partners shares a lane of the two
        /// vectors: 128-bit halves are exchanged for the layer of 8, 64-bit groups for the layer of 4, and 32-bit pairs
        /// for the layer of 2. The same steps in reverse restore the order.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LastLayers(ref Vector256<short> first, ref Vector256<short> second, ref short twiddles)
        {
            // [c0..c7, c16..c23] and [c8..c15, c24..c31]: partners 8 apart.
            Vector256<short> x = Avx2.Permute2x128(first, second, 0x20);
            Vector256<short> y = Avx2.Permute2x128(first, second, 0x31);
            LaneButterfly(ref x, ref y, ref twiddles);

            // [c0..c3, c8..c11, c16..c19, c24..c27] and [c4..c7, ...]: partners 4 apart.
            Vector256<short> x4 = Avx2.UnpackLow(x.AsInt64(), y.AsInt64()).AsInt16();
            Vector256<short> y4 = Avx2.UnpackHigh(x.AsInt64(), y.AsInt64()).AsInt16();
            LaneButterfly(ref x4, ref y4, ref Unsafe.Add(ref twiddles, LayerTwiddleShorts));

            // [c0, c1, c4, c5, c8, c9, ...] and [c2, c3, c6, c7, ...]: partners 2 apart.
            Vector256<short> x2 = Avx2.Blend(x4.AsInt32(), Avx2.ShiftLeftLogical(y4.AsInt64(), 32).AsInt32(), OddLanes).AsInt16();
            Vector256<short> y2 = Avx2.Blend(Avx2.ShiftRightLogical(x4.AsInt64(), 32).AsInt32(), y4.AsInt32(), OddLanes).AsInt16();
            LaneButterfly(ref x2, ref y2, ref Unsafe.Add(ref twiddles, 2 * LayerTwiddleShorts));

            x4 = Avx2.Blend(x2.AsInt32(), Avx2.ShiftLeftLogical(y2.AsInt64(), 32).AsInt32(), OddLanes).AsInt16();
            y4 = Avx2.Blend(Avx2.ShiftRightLogical(x2.AsInt64(), 32).AsInt32(), y2.AsInt32(), OddLanes).AsInt16();
            x = Avx2.UnpackLow(x4.AsInt64(), y4.AsInt64()).AsInt16();
            y = Avx2.UnpackHigh(x4.AsInt64(), y4.AsInt64()).AsInt16();
            first = Avx2.Permute2x128(x, y, 0x20);
            second = Avx2.Permute2x128(x, y, 0x31);
        }

        /// <summary>
        /// Applies the inverse transform's layers of 2, 4 and 8 to 32 consecutive coefficients.
        /// </summary>
        /// <param name="first">The first sixteen coefficients; receives them transformed.</param>
        /// <param name="second">The next sixteen; receives them transformed.</param>
        /// <param name="twiddles">
        /// The block's lane-ordered twiddles for the layers of 8, 4 and 2, in that order, as <see cref="LastLayers" />
        /// takes them.
        /// </param>
        /// <remarks>
        /// The rearrangements are those of <see cref="LastLayers" />, taken in the opposite order.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FirstInverseLayers(ref Vector256<short> first, ref Vector256<short> second, ref short twiddles)
        {
            Vector256<short> x = Avx2.Permute2x128(first, second, 0x20);
            Vector256<short> y = Avx2.Permute2x128(first, second, 0x31);
            Vector256<short> x4 = Avx2.UnpackLow(x.AsInt64(), y.AsInt64()).AsInt16();
            Vector256<short> y4 = Avx2.UnpackHigh(x.AsInt64(), y.AsInt64()).AsInt16();
            Vector256<short> x2 = Avx2.Blend(x4.AsInt32(), Avx2.ShiftLeftLogical(y4.AsInt64(), 32).AsInt32(), OddLanes).AsInt16();
            Vector256<short> y2 = Avx2.Blend(Avx2.ShiftRightLogical(x4.AsInt64(), 32).AsInt32(), y4.AsInt32(), OddLanes).AsInt16();
            InverseLaneButterfly(ref x2, ref y2, ref Unsafe.Add(ref twiddles, 2 * LayerTwiddleShorts));

            x4 = Avx2.Blend(x2.AsInt32(), Avx2.ShiftLeftLogical(y2.AsInt64(), 32).AsInt32(), OddLanes).AsInt16();
            y4 = Avx2.Blend(Avx2.ShiftRightLogical(x2.AsInt64(), 32).AsInt32(), y2.AsInt32(), OddLanes).AsInt16();
            InverseLaneButterfly(ref x4, ref y4, ref Unsafe.Add(ref twiddles, LayerTwiddleShorts));

            x = Avx2.UnpackLow(x4.AsInt64(), y4.AsInt64()).AsInt16();
            y = Avx2.UnpackHigh(x4.AsInt64(), y4.AsInt64()).AsInt16();
            InverseLaneButterfly(ref x, ref y, ref twiddles);

            first = Avx2.Permute2x128(x, y, 0x20);
            second = Avx2.Permute2x128(x, y, 0x31);
        }

        /// <summary>
        /// Loads sixteen int coefficients and narrows them to 16 bits, in order.
        /// </summary>
        /// <param name="source">The first coefficient of the polynomial.</param>
        /// <param name="offset">The index of the first of the sixteen.</param>
        /// <returns>The sixteen coefficients as 16-bit lanes.</returns>
        /// <remarks>
        /// Every coefficient fits in 16 bits, so the saturating pack leaves each unchanged; the pack interleaves the
        /// 128-bit halves, and the permutation restores their order.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<short> Pack(ref int source, nuint offset)
        {
            Vector256<short> packed = Avx2.PackSignedSaturate(Vector256.LoadUnsafe(ref source, offset), Vector256.LoadUnsafe(ref source, offset + 8));
            return Avx2.Permute4x64(packed.AsInt64(), 0b11_01_10_00).AsInt16();
        }

        /// <summary>
        /// Sign-extends sixteen 16-bit lanes to ints and stores them in order.
        /// </summary>
        /// <param name="value">The sixteen coefficients.</param>
        /// <param name="destination">The first coefficient of the polynomial.</param>
        /// <param name="offset">The index at which to store the first of the sixteen.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Unpack(Vector256<short> value, ref int destination, nuint offset)
        {
            Avx2.ConvertToVector256Int32(value.GetLower()).StoreUnsafe(ref destination, offset);
            Avx2.ConvertToVector256Int32(value.GetUpper()).StoreUnsafe(ref destination, offset + 8);
        }

        /// <summary>
        /// Loads sixteen pairs of int coefficients and returns their first members and their second members as two
        /// 16-bit vectors, pair order kept.
        /// </summary>
        /// <param name="source">The first coefficient of the polynomial.</param>
        /// <param name="offset">The index of the first of the 32 coefficients.</param>
        /// <param name="evens">Receives the pairs' first members.</param>
        /// <param name="odds">Receives the pairs' second members.</param>
        /// <remarks>
        /// Each pair is a 64-bit group of two ints. Masking keeps the first and shifting moves the second down, leaving
        /// one value per 64-bit group; two saturating packs bring sixteen of them into one vector, in the order the
        /// packs interleave, and a permutation of 32-bit lanes restores pair order. Every coefficient is below q, so
        /// the packs leave each unchanged.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SplitPairs(ref int source, nuint offset, out Vector256<short> evens, out Vector256<short> odds)
        {
            Vector256<int> v0 = Vector256.LoadUnsafe(ref source, offset);
            Vector256<int> v1 = Vector256.LoadUnsafe(ref source, offset + 8);
            Vector256<int> v2 = Vector256.LoadUnsafe(ref source, offset + 16);
            Vector256<int> v3 = Vector256.LoadUnsafe(ref source, offset + 24);
            Vector256<long> firstMember = Vector256.Create(0xFFFF_FFFFL);

            Vector256<int> evens01 = Avx2.PackSignedSaturate(Avx2.And(v0.AsInt64(), firstMember).AsInt32(), Avx2.And(v1.AsInt64(), firstMember).AsInt32()).AsInt32();
            Vector256<int> evens23 = Avx2.PackSignedSaturate(Avx2.And(v2.AsInt64(), firstMember).AsInt32(), Avx2.And(v3.AsInt64(), firstMember).AsInt32()).AsInt32();
            Vector256<int> odds01 = Avx2.PackSignedSaturate(Avx2.ShiftRightLogical(v0.AsInt64(), 32).AsInt32(), Avx2.ShiftRightLogical(v1.AsInt64(), 32).AsInt32()).AsInt32();
            Vector256<int> odds23 = Avx2.PackSignedSaturate(Avx2.ShiftRightLogical(v2.AsInt64(), 32).AsInt32(), Avx2.ShiftRightLogical(v3.AsInt64(), 32).AsInt32()).AsInt32();

            // Each 32-bit lane now holds one value; the second pack leaves pairs of pairs in the order
            // [p0 p1, p4 p5, p8 p9, p12 p13 | p2 p3, p6 p7, p10 p11, p14 p15], which the permutation sorts.
            Vector256<int> order = Vector256.Create(0, 4, 1, 5, 2, 6, 3, 7);
            evens = Avx2.PermuteVar8x32(Avx2.PackSignedSaturate(evens01, evens23).AsInt32(), order).AsInt16();
            odds = Avx2.PermuteVar8x32(Avx2.PackSignedSaturate(odds01, odds23).AsInt32(), order).AsInt16();
        }

        /// <summary>
        /// Builds the twiddle table, or its multiples of q⁻¹, narrowed to 16 bits.
        /// </summary>
        /// <param name="timesQInverse">
        /// <see langword="true" /> for ζ · q⁻¹ mod 2^16; <see langword="false" /> for ζ.
        /// </param>
        /// <returns>The 128-entry table.</returns>
        private static short[] BuildZetas16(bool timesQInverse)
        {
            short[] table = new short[s_zetas.Length];
            for (int i = 0; i < table.Length; i++)
                table[i] = unchecked((short)(timesQInverse ? s_zetas[i] * QInverse : s_zetas[i]));

            return table;
        }

        /// <summary>
        /// Builds the lane-ordered twiddles of the three layers the transforms rearrange coefficients for, for each of
        /// the eight blocks of 32 coefficients.
        /// </summary>
        /// <param name="forward">
        /// <see langword="true" /> for the forward transform's twiddles; <see langword="false" /> for the inverse's.
        /// </param>
        /// <returns>
        /// For each block, the layers of 8, 4 and 2 in that order, each as two vectors: the lanes' twiddles and those
        /// times q⁻¹ mod 2^16.
        /// </returns>
        /// <remarks>
        /// The twiddles are those the scalar transforms use: ζ[k + b] for block b of a layer with k blocks going
        /// forward, and ζ[2k − 1 − b] going back. Lane l holds the coefficient the rearrangement of
        /// <see cref="LastLayers" /> puts there.
        /// </remarks>
        private static short[] BuildLaneTwiddles(bool forward)
        {
            short[] table = new short[8 * 3 * LayerTwiddleShorts];
            for (int block = 0; block < 8; block++)
            {
                for (int layer = 0; layer < 3; layer++)
                {
                    int length = 8 >> layer;
                    Span<short> destination = table.AsSpan(((block * 3) + layer) * LayerTwiddleShorts, LayerTwiddleShorts);
                    for (int lane = 0; lane < Lanes; lane++)
                    {
                        // The coefficient of the block in each lane of the low vector, for the layers of 8, 4 and 2.
                        int coefficient = layer switch
                        {
                            0 => lane < 8 ? lane : 8 + lane,
                            1 => ((lane >> 2) * 8) + (lane & 3),
                            _ => ((lane >> 1) * 4) + (lane & 1),
                        };

                        int blocks = N / (2 * length);
                        int partnerBlock = ((32 * block) + coefficient) / (2 * length);
                        int zeta = s_zetas[forward ? blocks + partnerBlock : (2 * blocks) - 1 - partnerBlock];
                        destination[lane] = (short)zeta;
                        destination[Lanes + lane] = unchecked((short)(zeta * QInverse));
                    }
                }
            }

            return table;
        }

        /// <summary>
        /// Builds γ · 2^16 mod q and its multiple of q⁻¹ for each base-case pair, sixteen pairs per vector.
        /// </summary>
        /// <returns>For each group of sixteen pairs, the two vectors.</returns>
        private static short[] BuildGammas16()
        {
            short[] table = new short[8 * 2 * Lanes];
            for (int group = 0; group < 8; group++)
            {
                for (int lane = 0; lane < Lanes; lane++)
                {
                    int gamma = (int)(((long)s_gammas[(Lanes * group) + lane] << 16) % Q);
                    table[(group * 2 * Lanes) + lane] = (short)gamma;
                    table[(group * 2 * Lanes) + Lanes + lane] = unchecked((short)(gamma * QInverse));
                }
            }

            return table;
        }
    }
}
