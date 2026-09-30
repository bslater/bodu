// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngine.Vector256.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class MLDsaEngine
{
    /// <summary>
    /// Provides the ML-DSA transforms and coefficient-wise products over AVX2, eight coefficients per 256-bit vector,
    /// computing exactly the values the scalar code computes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each Montgomery product is formed as the reference implementation's AVX2 code forms it: <c>VPMULDQ</c>
    /// multiplies the even and the odd lanes into 64-bit products, a second multiplication by q⁻¹ gives each lane's
    /// multiple m, and the high halves of a·ζ and m·q are subtracted. Their low halves are equal by the choice of m, so
    /// the difference of the high halves is exactly <see cref="MontgomeryReduce" /> of the product, bit for bit.
    /// </para>
    /// <para>
    /// Each transform makes two passes over memory. The first three layers of the forward transform run on eight
    /// vectors 32 coefficients apart, and the other five on each block of 32 consecutive coefficients, the last three
    /// of them after an in-register rearrangement that gives each pair of partners one lane. The inverse runs the same
    /// passes in the opposite order. Every layer performs the scalar code's additions, subtractions and reductions in
    /// the same order, so the ranges the scalar code documents hold here too, and so does its output, canonical in the
    /// same places.
    /// </para>
    /// <para>
    /// No coefficient is ever used as an index or a branch condition.
    /// </para>
    /// </remarks>
    internal static class Vector256Kernel
    {
        /// <summary>The number of coefficients in each vector.</summary>
        private const int Lanes = 8;

        /// <summary>The blend mask that takes the odd lanes from the second operand.</summary>
        private const byte OddLanes = 0b1010_1010;

        /// <summary>The number of ints each group of lane-ordered twiddles takes: four vectors.</summary>
        private const int GroupTwiddleInts = 4 * Lanes;

        /// <summary>ζ · q⁻¹ mod 2^32 for every entry of the twiddle table, the second factor of each broadcast product.</summary>
        private static readonly int[] s_zetasTimesQInverse = BuildZetasTimesQInverse();

        /// <summary>The forward transform's twiddles for its last three layers, lane by lane.</summary>
        private static readonly int[] s_forwardTwiddles = BuildLaneTwiddles(forward: true);

        /// <summary>The inverse transform's twiddles for its first three layers, lane by lane.</summary>
        private static readonly int[] s_inverseTwiddles = BuildLaneTwiddles(forward: false);

        /// <summary>
        /// Applies the forward NTT to 256 coefficients in place, as
        /// <see cref="MLDsaEngine.Ntt(KernelKind, Span{int})" /> documents.
        /// </summary>
        /// <param name="coefficients">The first of the 256 coefficients.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void Ntt(ref int coefficients)
        {
            ref int zetas = ref MemoryMarshal.GetArrayDataReference(s_zetas);
            ref int zetasQ = ref MemoryMarshal.GetArrayDataReference(s_zetasTimesQInverse);

            // Layers of 128, 64 and 32: eight vectors, 32 coefficients apart, stay in registers for all three.
            for (int j = 0; j < 32; j += Lanes)
            {
                ref int first = ref Unsafe.Add(ref coefficients, j);
                Vector256<int> v0 = Vector256.LoadUnsafe(ref first);
                Vector256<int> v1 = Vector256.LoadUnsafe(ref first, 32);
                Vector256<int> v2 = Vector256.LoadUnsafe(ref first, 64);
                Vector256<int> v3 = Vector256.LoadUnsafe(ref first, 96);
                Vector256<int> v4 = Vector256.LoadUnsafe(ref first, 128);
                Vector256<int> v5 = Vector256.LoadUnsafe(ref first, 160);
                Vector256<int> v6 = Vector256.LoadUnsafe(ref first, 192);
                Vector256<int> v7 = Vector256.LoadUnsafe(ref first, 224);

                Butterfly(ref v0, ref v4, ref zetas, ref zetasQ, 1);
                Butterfly(ref v1, ref v5, ref zetas, ref zetasQ, 1);
                Butterfly(ref v2, ref v6, ref zetas, ref zetasQ, 1);
                Butterfly(ref v3, ref v7, ref zetas, ref zetasQ, 1);

                Butterfly(ref v0, ref v2, ref zetas, ref zetasQ, 2);
                Butterfly(ref v1, ref v3, ref zetas, ref zetasQ, 2);
                Butterfly(ref v4, ref v6, ref zetas, ref zetasQ, 3);
                Butterfly(ref v5, ref v7, ref zetas, ref zetasQ, 3);

                Butterfly(ref v0, ref v1, ref zetas, ref zetasQ, 4);
                Butterfly(ref v2, ref v3, ref zetas, ref zetasQ, 5);
                Butterfly(ref v4, ref v5, ref zetas, ref zetasQ, 6);
                Butterfly(ref v6, ref v7, ref zetas, ref zetasQ, 7);

                v0.StoreUnsafe(ref first);
                v1.StoreUnsafe(ref first, 32);
                v2.StoreUnsafe(ref first, 64);
                v3.StoreUnsafe(ref first, 96);
                v4.StoreUnsafe(ref first, 128);
                v5.StoreUnsafe(ref first, 160);
                v6.StoreUnsafe(ref first, 192);
                v7.StoreUnsafe(ref first, 224);
            }

            // Layers of 16, 8, 4, 2 and 1 on each block of 32 consecutive coefficients, reduced as they are stored.
            ref int twiddles = ref MemoryMarshal.GetArrayDataReference(s_forwardTwiddles);
            for (int block = 0; block < 8; block++)
            {
                ref int first = ref Unsafe.Add(ref coefficients, 32 * block);
                Vector256<int> v0 = Vector256.LoadUnsafe(ref first);
                Vector256<int> v1 = Vector256.LoadUnsafe(ref first, 8);
                Vector256<int> v2 = Vector256.LoadUnsafe(ref first, 16);
                Vector256<int> v3 = Vector256.LoadUnsafe(ref first, 24);

                Butterfly(ref v0, ref v2, ref zetas, ref zetasQ, 8 + block);
                Butterfly(ref v1, ref v3, ref zetas, ref zetasQ, 8 + block);
                Butterfly(ref v0, ref v1, ref zetas, ref zetasQ, 16 + (2 * block));
                Butterfly(ref v2, ref v3, ref zetas, ref zetasQ, 17 + (2 * block));

                LastLayers(ref v0, ref v1, ref Unsafe.Add(ref twiddles, 2 * block * 3 * GroupTwiddleInts));
                LastLayers(ref v2, ref v3, ref Unsafe.Add(ref twiddles, ((2 * block) + 1) * 3 * GroupTwiddleInts));

                Freeze(v0).StoreUnsafe(ref first);
                Freeze(v1).StoreUnsafe(ref first, 8);
                Freeze(v2).StoreUnsafe(ref first, 16);
                Freeze(v3).StoreUnsafe(ref first, 24);
            }
        }

        /// <summary>
        /// Applies the inverse NTT to 256 coefficients in place, including the final scaling by 256⁻¹, as
        /// <see cref="MLDsaEngine.InvNtt(KernelKind, Span{int})" /> documents.
        /// </summary>
        /// <param name="coefficients">The first of the 256 coefficients.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void InvNtt(ref int coefficients)
        {
            ref int zetas = ref MemoryMarshal.GetArrayDataReference(s_zetas);
            ref int zetasQ = ref MemoryMarshal.GetArrayDataReference(s_zetasTimesQInverse);

            // Layers of 1, 2, 4, 8 and 16 on each block of 32 consecutive coefficients.
            ref int twiddles = ref MemoryMarshal.GetArrayDataReference(s_inverseTwiddles);
            for (int block = 0; block < 8; block++)
            {
                ref int first = ref Unsafe.Add(ref coefficients, 32 * block);
                Vector256<int> v0 = Vector256.LoadUnsafe(ref first);
                Vector256<int> v1 = Vector256.LoadUnsafe(ref first, 8);
                Vector256<int> v2 = Vector256.LoadUnsafe(ref first, 16);
                Vector256<int> v3 = Vector256.LoadUnsafe(ref first, 24);

                FirstInverseLayers(ref v0, ref v1, ref Unsafe.Add(ref twiddles, 2 * block * 3 * GroupTwiddleInts));
                FirstInverseLayers(ref v2, ref v3, ref Unsafe.Add(ref twiddles, ((2 * block) + 1) * 3 * GroupTwiddleInts));

                InverseButterfly(ref v0, ref v1, ref zetas, ref zetasQ, 31 - (2 * block));
                InverseButterfly(ref v2, ref v3, ref zetas, ref zetasQ, 30 - (2 * block));
                InverseButterfly(ref v0, ref v2, ref zetas, ref zetasQ, 15 - block);
                InverseButterfly(ref v1, ref v3, ref zetas, ref zetasQ, 15 - block);

                v0.StoreUnsafe(ref first);
                v1.StoreUnsafe(ref first, 8);
                v2.StoreUnsafe(ref first, 16);
                v3.StoreUnsafe(ref first, 24);
            }

            // Layers of 32 and 64, then the last, which carries the scaling by 256⁻¹, on eight vectors 32 apart.
            int scaledZeta = MontgomeryReduce(s_zetas[1] * InverseOf256Montgomery);
            for (int j = 0; j < 32; j += Lanes)
            {
                ref int first = ref Unsafe.Add(ref coefficients, j);
                Vector256<int> v0 = Vector256.LoadUnsafe(ref first);
                Vector256<int> v1 = Vector256.LoadUnsafe(ref first, 32);
                Vector256<int> v2 = Vector256.LoadUnsafe(ref first, 64);
                Vector256<int> v3 = Vector256.LoadUnsafe(ref first, 96);
                Vector256<int> v4 = Vector256.LoadUnsafe(ref first, 128);
                Vector256<int> v5 = Vector256.LoadUnsafe(ref first, 160);
                Vector256<int> v6 = Vector256.LoadUnsafe(ref first, 192);
                Vector256<int> v7 = Vector256.LoadUnsafe(ref first, 224);

                InverseButterfly(ref v0, ref v1, ref zetas, ref zetasQ, 7);
                InverseButterfly(ref v2, ref v3, ref zetas, ref zetasQ, 6);
                InverseButterfly(ref v4, ref v5, ref zetas, ref zetasQ, 5);
                InverseButterfly(ref v6, ref v7, ref zetas, ref zetasQ, 4);

                InverseButterfly(ref v0, ref v2, ref zetas, ref zetasQ, 3);
                InverseButterfly(ref v1, ref v3, ref zetas, ref zetasQ, 3);
                InverseButterfly(ref v4, ref v6, ref zetas, ref zetasQ, 2);
                InverseButterfly(ref v5, ref v7, ref zetas, ref zetasQ, 2);

                ScaledButterfly(ref v0, ref v4, scaledZeta);
                ScaledButterfly(ref v1, ref v5, scaledZeta);
                ScaledButterfly(ref v2, ref v6, scaledZeta);
                ScaledButterfly(ref v3, ref v7, scaledZeta);

                v0.StoreUnsafe(ref first);
                v1.StoreUnsafe(ref first, 32);
                v2.StoreUnsafe(ref first, 64);
                v3.StoreUnsafe(ref first, 96);
                v4.StoreUnsafe(ref first, 128);
                v5.StoreUnsafe(ref first, 160);
                v6.StoreUnsafe(ref first, 192);
                v7.StoreUnsafe(ref first, 224);
            }
        }

        /// <summary>
        /// Multiplies two NTT-domain polynomials coefficient-wise, as
        /// <see cref="MLDsaEngine.MultiplyNtt(KernelKind, ReadOnlySpan{int}, ReadOnlySpan{int}, Span{int})" />
        /// documents.
        /// </summary>
        /// <param name="montgomeryLeft">The first of the 256 coefficients of the factor in Montgomery form.</param>
        /// <param name="right">The first of the 256 coefficients of the other factor.</param>
        /// <param name="destination">The first of the 256 coefficients receiving the product.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void MultiplyNtt(ref int montgomeryLeft, ref int right, ref int destination)
        {
            for (nuint i = 0; i < N; i += Lanes)
                MultiplyReduce(Vector256.LoadUnsafe(ref montgomeryLeft, i), Vector256.LoadUnsafe(ref right, i)).StoreUnsafe(ref destination, i);
        }

        /// <summary>
        /// Adds the coefficient-wise product of two NTT-domain polynomials into an accumulator, as
        /// <see cref="MLDsaEngine.MultiplyAccumulateNtt(KernelKind, ReadOnlySpan{int}, ReadOnlySpan{int}, Span{int})" />
        /// documents.
        /// </summary>
        /// <param name="montgomeryLeft">The first of the 256 coefficients of the factor in Montgomery form.</param>
        /// <param name="right">The first of the 256 coefficients of the other factor.</param>
        /// <param name="accumulator">The first of the 256 coefficients the product is added into.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void MultiplyAccumulateNtt(ref int montgomeryLeft, ref int right, ref int accumulator)
        {
            for (nuint i = 0; i < N; i += Lanes)
            {
                Vector256<int> product = MultiplyReduce(Vector256.LoadUnsafe(ref montgomeryLeft, i), Vector256.LoadUnsafe(ref right, i));
                Avx2.Add(Vector256.LoadUnsafe(ref accumulator, i), product).StoreUnsafe(ref accumulator, i);
            }
        }

        /// <summary>
        /// Adds two polynomials coefficient-wise modulo q, as
        /// <see cref="MLDsaEngine.AddModQ(KernelKind, ReadOnlySpan{int}, ReadOnlySpan{int}, Span{int})" /> documents.
        /// </summary>
        /// <param name="left">The first of the 256 coefficients of the first polynomial.</param>
        /// <param name="right">The first of the 256 coefficients of the second polynomial.</param>
        /// <param name="sum">The first of the 256 coefficients receiving the sum; may be either operand.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void AddModQ(ref int left, ref int right, ref int sum)
        {
            Vector256<int> q = Vector256.Create(Q);
            for (nuint i = 0; i < N; i += Lanes)
                Canonicalize(Avx2.Subtract(Avx2.Add(Vector256.LoadUnsafe(ref left, i), Vector256.LoadUnsafe(ref right, i)), q)).StoreUnsafe(ref sum, i);
        }

        /// <summary>
        /// Subtracts one polynomial from another coefficient-wise modulo q, as
        /// <see cref="MLDsaEngine.SubtractModQ(KernelKind, ReadOnlySpan{int}, ReadOnlySpan{int}, Span{int})" />
        /// documents.
        /// </summary>
        /// <param name="left">The first of the 256 coefficients of the minuend.</param>
        /// <param name="right">The first of the 256 coefficients of the subtrahend.</param>
        /// <param name="difference">
        /// The first of the 256 coefficients receiving the difference; may be either operand.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void SubtractModQ(ref int left, ref int right, ref int difference)
        {
            for (nuint i = 0; i < N; i += Lanes)
                Canonicalize(Avx2.Subtract(Vector256.LoadUnsafe(ref left, i), Vector256.LoadUnsafe(ref right, i))).StoreUnsafe(ref difference, i);
        }

        /// <summary>
        /// Returns the infinity norm of a polynomial, as
        /// <see cref="MLDsaEngine.InfinityNorm(KernelKind, ReadOnlySpan{int})" /> documents.
        /// </summary>
        /// <param name="poly">The first of the 256 coefficients.</param>
        /// <returns>The largest magnitude of the coefficients' centered representatives.</returns>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static int InfinityNorm(ref int poly)
        {
            Vector256<int> q = Vector256.Create(Q);
            Vector256<int> half = Vector256.Create((Q - 1) / 2);
            Vector256<int> maximum = Vector256<int>.Zero;

            for (nuint i = 0; i < N; i += Lanes)
            {
                // A coefficient above (q − 1) / 2 is a negative one folded modulo q, whose magnitude is q minus it.
                Vector256<int> value = Vector256.LoadUnsafe(ref poly, i);
                Vector256<int> negative = Avx2.ShiftRightArithmetic(Avx2.Subtract(half, value), 31);
                Vector256<int> centered = Avx2.Subtract(value, Avx2.And(Avx2.Subtract(Avx2.ShiftLeftLogical(value, 1), q), negative));
                maximum = Avx2.Max(maximum, centered);
            }

            return MaxAcrossLanes(maximum);
        }

        /// <summary>
        /// Returns the largest magnitude of the coefficients' low parts, as
        /// <see cref="MLDsaEngine.LowBitsNorm(KernelKind, int, ReadOnlySpan{int})" /> documents.
        /// </summary>
        /// <param name="gamma2">The parameter γ₂: (q − 1) / 32 or (q − 1) / 88.</param>
        /// <param name="r">The first of the 256 coefficients.</param>
        /// <returns>The largest magnitude.</returns>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static int LowBitsNorm(int gamma2, ref int r)
        {
            bool wide = gamma2 == (Q - 1) / 32;
            Vector256<int> alpha = Vector256.Create(2 * gamma2);
            Vector256<int> q = Vector256.Create(Q);
            Vector256<int> half = Vector256.Create((Q - 1) / 2);
            Vector256<int> maximum = Vector256<int>.Zero;

            for (nuint i = 0; i < N; i += Lanes)
            {
                // r₀ = r − r₁·2γ₂, folded down by q where it exceeds (q − 1) / 2, as Decompose computes it.
                Vector256<int> value = Vector256.LoadUnsafe(ref r, i);
                Vector256<int> low = Avx2.Subtract(value, Avx2.MultiplyLow(HighBits(value, wide), alpha));
                low = Avx2.Subtract(low, Avx2.And(Avx2.ShiftRightArithmetic(Avx2.Subtract(half, low), 31), q));
                maximum = Avx2.Max(maximum, Avx2.Abs(low).AsInt32());
            }

            return MaxAcrossLanes(maximum);
        }

        /// <summary>
        /// Replaces each coefficient of a polynomial by its high part, as
        /// <see cref="MLDsaEngine.HighBits(KernelKind, int, ReadOnlySpan{int}, Span{int})" /> documents.
        /// </summary>
        /// <param name="gamma2">The parameter γ₂: (q − 1) / 32 or (q − 1) / 88.</param>
        /// <param name="r">The first of the 256 coefficients.</param>
        /// <param name="r1">
        /// The first of the 256 coefficients receiving the high parts; may be <paramref name="r" />.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void HighBits(int gamma2, ref int r, ref int r1)
        {
            bool wide = gamma2 == (Q - 1) / 32;
            for (nuint i = 0; i < N; i += Lanes)
                HighBits(Vector256.LoadUnsafe(ref r, i), wide).StoreUnsafe(ref r1, i);
        }

        /// <summary>
        /// Computes a polynomial's hints and returns their number, as
        /// <see cref="MLDsaEngine.MakeHints(KernelKind, int, ReadOnlySpan{int}, ReadOnlySpan{int}, Span{int})" />
        /// documents.
        /// </summary>
        /// <param name="gamma2">The parameter γ₂: (q − 1) / 32 or (q − 1) / 88.</param>
        /// <param name="ct0">The first of the 256 coefficients of ct₀.</param>
        /// <param name="wMinusCs2">The first of the 256 coefficients of w − cs₂.</param>
        /// <param name="hints">The first of the 256 coefficients receiving the hints, each 0 or 1.</param>
        /// <returns>The number of hints that are 1.</returns>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static int MakeHints(int gamma2, ref int ct0, ref int wMinusCs2, ref int hints)
        {
            bool wide = gamma2 == (Q - 1) / 32;
            Vector256<int> q = Vector256.Create(Q);
            Vector256<int> qMinusOne = Vector256.Create(Q - 1);
            Vector256<int> weight = Vector256<int>.Zero;

            for (nuint i = 0; i < N; i += Lanes)
            {
                Vector256<int> product = Vector256.LoadUnsafe(ref ct0, i);
                Vector256<int> negated = Canonicalize(Avx2.Subtract(Vector256<int>.Zero, product));
                Vector256<int> basis = Canonicalize(Avx2.Subtract(Avx2.Add(Vector256.LoadUnsafe(ref wMinusCs2, i), product), q));

                // MakeHint(γ₂, −ct₀, w − cs₂ + ct₀): whether adding the first to the second modulo q changes the high part.
                Vector256<int> sum = Avx2.Add(basis, negated);
                sum = Avx2.Subtract(sum, Avx2.And(q, Avx2.ShiftRightArithmetic(Avx2.Subtract(qMinusOne, sum), 31)));
                Vector256<int> difference = Avx2.Subtract(HighBits(basis, wide), HighBits(sum, wide));
                Vector256<int> hint = Avx2.ShiftRightLogical(Avx2.Or(difference, Avx2.Subtract(Vector256<int>.Zero, difference)), 31);

                hint.StoreUnsafe(ref hints, i);
                weight = Avx2.Add(weight, hint);
            }

            return Vector256.Sum(weight);
        }

        /// <summary>
        /// Converts every coefficient of a polynomial to Montgomery form, in place, as
        /// <see cref="MLDsaEngine.ToMontgomery(KernelKind, Span{int})" /> documents.
        /// </summary>
        /// <param name="poly">The first of the 256 coefficients.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void ToMontgomery(ref int poly)
        {
            Vector256<int> factor = Vector256.Create((int)MontgomerySquared);
            for (nuint i = 0; i < N; i += Lanes)
                MultiplyReduce(Vector256.LoadUnsafe(ref poly, i), factor).StoreUnsafe(ref poly, i);
        }

        /// <summary>
        /// Reduces every coefficient of a polynomial with <see cref="MLDsaEngine.Reduce32(int)" />, in place, as
        /// <see cref="MLDsaEngine.Reduce32(KernelKind, Span{int})" /> documents.
        /// </summary>
        /// <param name="poly">The first of the 256 coefficients.</param>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void Reduce32(ref int poly)
        {
            for (nuint i = 0; i < N; i += Lanes)
                Reduce32(Vector256.LoadUnsafe(ref poly, i)).StoreUnsafe(ref poly, i);
        }

        /// <summary>
        /// Returns <see cref="MontgomeryReduce" /> of the product of each lane of <paramref name="value" /> with its
        /// twiddle, the twiddles supplied for the even and the odd lanes with their multiples of q⁻¹.
        /// </summary>
        /// <param name="value">The coefficients.</param>
        /// <param name="evenZetas">The even lanes' twiddles, each in its lane's low 32 bits of a 64-bit pair.</param>
        /// <param name="evenZetasQ">The even lanes' twiddles times q⁻¹ mod 2^32, laid out alike.</param>
        /// <param name="oddZetas">The odd lanes' twiddles, each in the low 32 bits of its lane's 64-bit pair.</param>
        /// <param name="oddZetasQ">The odd lanes' twiddles times q⁻¹ mod 2^32, laid out alike.</param>
        /// <returns>The reduced products, each in (−q, q).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<int> MultiplyTwiddles(
            Vector256<int> value,
            Vector256<int> evenZetas,
            Vector256<int> evenZetasQ,
            Vector256<int> oddZetas,
            Vector256<int> oddZetasQ)
        {
            Vector256<int> q = Vector256.Create(Q);
            Vector256<int> odd = Avx2.ShiftRightLogical(value.AsInt64(), 32).AsInt32();

            // VPMULDQ reads the low 32 bits of each 64-bit pair, sign-extended: the even lanes of value, and of odd,
            // which holds the odd lanes shifted down.
            Vector256<long> evenProducts = Avx2.Multiply(value, evenZetas);
            Vector256<long> oddProducts = Avx2.Multiply(odd, oddZetas);
            Vector256<long> evenMultiples = Avx2.Multiply(Avx2.Multiply(value, evenZetasQ).AsInt32(), q);
            Vector256<long> oddMultiples = Avx2.Multiply(Avx2.Multiply(odd, oddZetasQ).AsInt32(), q);

            // The differences are multiples of 2^32; their high halves are the results, moved back to their own lanes.
            return Avx2.Blend(
                Avx2.ShiftRightLogical(Avx2.Subtract(evenProducts, evenMultiples), 32).AsInt32(),
                Avx2.Subtract(oddProducts, oddMultiples).AsInt32(),
                OddLanes);
        }

        /// <summary>
        /// Returns <see cref="MontgomeryReduce" /> of the product of each lane of <paramref name="left" /> with the
        /// same lane of <paramref name="right" />.
        /// </summary>
        /// <param name="left">The first factors.</param>
        /// <param name="right">The second factors.</param>
        /// <returns>The reduced products, each in (−q, q).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<int> MultiplyReduce(Vector256<int> left, Vector256<int> right)
        {
            Vector256<int> q = Vector256.Create(Q);
            Vector256<int> qInverse = Vector256.Create(QInverse);
            Vector256<int> leftOdd = Avx2.ShiftRightLogical(left.AsInt64(), 32).AsInt32();
            Vector256<int> rightOdd = Avx2.ShiftRightLogical(right.AsInt64(), 32).AsInt32();

            Vector256<long> evenProducts = Avx2.Multiply(left, right);
            Vector256<long> oddProducts = Avx2.Multiply(leftOdd, rightOdd);

            // Each multiple m is the low 32 bits of the product times q⁻¹, the low 32 bits of which VPMULDQ reads.
            Vector256<long> evenMultiples = Avx2.Multiply(Avx2.Multiply(evenProducts.AsInt32(), qInverse).AsInt32(), q);
            Vector256<long> oddMultiples = Avx2.Multiply(Avx2.Multiply(oddProducts.AsInt32(), qInverse).AsInt32(), q);

            return Avx2.Blend(
                Avx2.ShiftRightLogical(Avx2.Subtract(evenProducts, evenMultiples), 32).AsInt32(),
                Avx2.Subtract(oddProducts, oddMultiples).AsInt32(),
                OddLanes);
        }

        /// <summary>
        /// Applies one forward butterfly to two vectors of partners with a twiddle shared by every lane.
        /// </summary>
        /// <param name="low">The low partners; receives their sums.</param>
        /// <param name="high">The high partners; receives their differences.</param>
        /// <param name="zetas">The first entry of the twiddle table.</param>
        /// <param name="zetasQ">The first entry of the table of twiddles times q⁻¹.</param>
        /// <param name="index">The twiddle's index.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Butterfly(ref Vector256<int> low, ref Vector256<int> high, ref int zetas, ref int zetasQ, int index)
        {
            Vector256<int> zeta = Vector256.Create(Unsafe.Add(ref zetas, index));
            Vector256<int> zetaQ = Vector256.Create(Unsafe.Add(ref zetasQ, index));
            Vector256<int> t = MultiplyTwiddles(high, zeta, zetaQ, zeta, zetaQ);
            high = Avx2.Subtract(low, t);
            low = Avx2.Add(low, t);
        }

        /// <summary>
        /// Applies one inverse butterfly to two vectors of partners with a twiddle shared by every lane.
        /// </summary>
        /// <param name="low">The low partners; receives their sums.</param>
        /// <param name="high">The high partners; receives their reduced, twiddled differences.</param>
        /// <param name="zetas">The first entry of the twiddle table.</param>
        /// <param name="zetasQ">The first entry of the table of twiddles times q⁻¹.</param>
        /// <param name="index">The twiddle's index.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InverseButterfly(ref Vector256<int> low, ref Vector256<int> high, ref int zetas, ref int zetasQ, int index)
        {
            Vector256<int> zeta = Vector256.Create(Unsafe.Add(ref zetas, index));
            Vector256<int> zetaQ = Vector256.Create(Unsafe.Add(ref zetasQ, index));
            Vector256<int> t = low;
            low = Avx2.Add(t, high);
            high = MultiplyTwiddles(Avx2.Subtract(high, t), zeta, zetaQ, zeta, zetaQ);
        }

        /// <summary>
        /// Applies the inverse transform's last butterfly, which carries the scaling by 256⁻¹, and canonicalizes both
        /// results.
        /// </summary>
        /// <param name="low">The low partners; receives their scaled sums in [0, q).</param>
        /// <param name="high">The high partners; receives their scaled, twiddled differences in [0, q).</param>
        /// <param name="scaledZeta">The last layer's twiddle combined with 256⁻¹, in Montgomery form.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ScaledButterfly(ref Vector256<int> low, ref Vector256<int> high, int scaledZeta)
        {
            Vector256<int> scale = Vector256.Create((int)InverseOf256Montgomery);
            Vector256<int> scaleQ = Vector256.Create(unchecked((int)InverseOf256Montgomery * QInverse));
            Vector256<int> zeta = Vector256.Create(scaledZeta);
            Vector256<int> zetaQ = Vector256.Create(unchecked(scaledZeta * QInverse));
            Vector256<int> t = low;
            Vector256<int> u = high;
            low = Canonicalize(MultiplyTwiddles(Avx2.Add(t, u), scale, scaleQ, scale, scaleQ));
            high = Canonicalize(MultiplyTwiddles(Avx2.Subtract(u, t), zeta, zetaQ, zeta, zetaQ));
        }

        /// <summary>
        /// Applies one forward butterfly to two vectors of partners, each lane with its own twiddle.
        /// </summary>
        /// <param name="low">The low partners; receives their sums.</param>
        /// <param name="high">The high partners; receives their differences.</param>
        /// <param name="twiddles">The group's four vectors of lane-ordered twiddles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LaneButterfly(ref Vector256<int> low, ref Vector256<int> high, ref int twiddles)
        {
            Vector256<int> t = MultiplyTwiddles(
                high,
                Vector256.LoadUnsafe(ref twiddles),
                Vector256.LoadUnsafe(ref twiddles, Lanes),
                Vector256.LoadUnsafe(ref twiddles, 2 * Lanes),
                Vector256.LoadUnsafe(ref twiddles, 3 * Lanes));
            high = Avx2.Subtract(low, t);
            low = Avx2.Add(low, t);
        }

        /// <summary>
        /// Applies one inverse butterfly to two vectors of partners, each lane with its own twiddle.
        /// </summary>
        /// <param name="low">The low partners; receives their sums.</param>
        /// <param name="high">The high partners; receives their reduced, twiddled differences.</param>
        /// <param name="twiddles">The group's four vectors of lane-ordered twiddles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InverseLaneButterfly(ref Vector256<int> low, ref Vector256<int> high, ref int twiddles)
        {
            Vector256<int> t = low;
            low = Avx2.Add(t, high);
            high = MultiplyTwiddles(
                Avx2.Subtract(high, t),
                Vector256.LoadUnsafe(ref twiddles),
                Vector256.LoadUnsafe(ref twiddles, Lanes),
                Vector256.LoadUnsafe(ref twiddles, 2 * Lanes),
                Vector256.LoadUnsafe(ref twiddles, 3 * Lanes));
        }

        /// <summary>
        /// Applies the forward transform's layers of 4, 2 and 1 to sixteen consecutive coefficients.
        /// </summary>
        /// <param name="first">The first eight coefficients; receives them transformed.</param>
        /// <param name="second">The next eight; receives them transformed.</param>
        /// <param name="twiddles">The group's lane-ordered twiddles for the three layers, in that order.</param>
        /// <remarks>
        /// Before each layer the coefficients are rearranged so that each pair of partners shares a lane of the two
        /// vectors: 128-bit halves are exchanged for the layer of 4, 64-bit pairs for the layer of 2, and 32-bit lanes
        /// for the layer of 1. The same steps in reverse restore the order.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LastLayers(ref Vector256<int> first, ref Vector256<int> second, ref int twiddles)
        {
            // [c0..c3, c8..c11] and [c4..c7, c12..c15]: partners 4 apart.
            Vector256<int> x = Avx2.Permute2x128(first, second, 0x20);
            Vector256<int> y = Avx2.Permute2x128(first, second, 0x31);
            LaneButterfly(ref x, ref y, ref twiddles);

            // [c0, c1, c4, c5, c8, c9, c12, c13] and [c2, c3, c6, c7, ...]: partners 2 apart.
            Vector256<int> x2 = Avx2.UnpackLow(x.AsInt64(), y.AsInt64()).AsInt32();
            Vector256<int> y2 = Avx2.UnpackHigh(x.AsInt64(), y.AsInt64()).AsInt32();
            LaneButterfly(ref x2, ref y2, ref Unsafe.Add(ref twiddles, GroupTwiddleInts));

            // The even coefficients and the odd ones: partners 1 apart.
            Vector256<int> x1 = Avx2.Blend(x2, Avx2.ShiftLeftLogical(y2.AsInt64(), 32).AsInt32(), OddLanes);
            Vector256<int> y1 = Avx2.Blend(Avx2.ShiftRightLogical(x2.AsInt64(), 32).AsInt32(), y2, OddLanes);
            LaneButterfly(ref x1, ref y1, ref Unsafe.Add(ref twiddles, 2 * GroupTwiddleInts));

            x2 = Avx2.Blend(x1, Avx2.ShiftLeftLogical(y1.AsInt64(), 32).AsInt32(), OddLanes);
            y2 = Avx2.Blend(Avx2.ShiftRightLogical(x1.AsInt64(), 32).AsInt32(), y1, OddLanes);
            x = Avx2.UnpackLow(x2.AsInt64(), y2.AsInt64()).AsInt32();
            y = Avx2.UnpackHigh(x2.AsInt64(), y2.AsInt64()).AsInt32();
            first = Avx2.Permute2x128(x, y, 0x20);
            second = Avx2.Permute2x128(x, y, 0x31);
        }

        /// <summary>
        /// Applies the inverse transform's layers of 1, 2 and 4 to sixteen consecutive coefficients.
        /// </summary>
        /// <param name="first">The first eight coefficients; receives them transformed.</param>
        /// <param name="second">The next eight; receives them transformed.</param>
        /// <param name="twiddles">
        /// The group's lane-ordered twiddles for the layers of 4, 2 and 1, in that order, as <see cref="LastLayers" />
        /// takes them.
        /// </param>
        /// <remarks>
        /// The rearrangements are those of <see cref="LastLayers" />, taken in the opposite order.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FirstInverseLayers(ref Vector256<int> first, ref Vector256<int> second, ref int twiddles)
        {
            Vector256<int> x = Avx2.Permute2x128(first, second, 0x20);
            Vector256<int> y = Avx2.Permute2x128(first, second, 0x31);
            Vector256<int> x2 = Avx2.UnpackLow(x.AsInt64(), y.AsInt64()).AsInt32();
            Vector256<int> y2 = Avx2.UnpackHigh(x.AsInt64(), y.AsInt64()).AsInt32();
            Vector256<int> x1 = Avx2.Blend(x2, Avx2.ShiftLeftLogical(y2.AsInt64(), 32).AsInt32(), OddLanes);
            Vector256<int> y1 = Avx2.Blend(Avx2.ShiftRightLogical(x2.AsInt64(), 32).AsInt32(), y2, OddLanes);
            InverseLaneButterfly(ref x1, ref y1, ref Unsafe.Add(ref twiddles, 2 * GroupTwiddleInts));

            x2 = Avx2.Blend(x1, Avx2.ShiftLeftLogical(y1.AsInt64(), 32).AsInt32(), OddLanes);
            y2 = Avx2.Blend(Avx2.ShiftRightLogical(x1.AsInt64(), 32).AsInt32(), y1, OddLanes);
            InverseLaneButterfly(ref x2, ref y2, ref Unsafe.Add(ref twiddles, GroupTwiddleInts));

            x = Avx2.UnpackLow(x2.AsInt64(), y2.AsInt64()).AsInt32();
            y = Avx2.UnpackHigh(x2.AsInt64(), y2.AsInt64()).AsInt32();
            InverseLaneButterfly(ref x, ref y, ref twiddles);

            first = Avx2.Permute2x128(x, y, 0x20);
            second = Avx2.Permute2x128(x, y, 0x31);
        }

        /// <summary>
        /// Returns <see cref="Freeze" /> of each lane: its representative in [0, q).
        /// </summary>
        /// <param name="value">The coefficients, each from −2^31 to 2^31 − 2^22 − 1.</param>
        /// <returns>The canonical representatives.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<int> Freeze(Vector256<int> value) =>
            Canonicalize(Reduce32(value));

        /// <summary>
        /// Returns <see cref="MLDsaEngine.Reduce32(int)" /> of each lane: a representative within (−q, q).
        /// </summary>
        /// <param name="value">The coefficients, each from −2^31 to 2^31 − 2^22 − 1.</param>
        /// <returns>The reduced representatives.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<int> Reduce32(Vector256<int> value)
        {
            Vector256<int> quotient = Avx2.ShiftRightArithmetic(Avx2.Add(value, Vector256.Create(1 << 22)), 23);
            return Avx2.Subtract(value, Avx2.MultiplyLow(quotient, Vector256.Create(Q)));
        }

        /// <summary>
        /// Returns <see cref="MLDsaEngine.HighBits(int, int)" /> of each lane.
        /// </summary>
        /// <param name="r">The coefficients, each in [0, q).</param>
        /// <param name="wide">
        /// <see langword="true" /> for γ₂ = (q − 1) / 32; <see langword="false" /> for γ₂ = (q − 1) / 88.
        /// </param>
        /// <returns>The high parts.</returns>
        /// <remarks>
        /// Each product fits in 32 bits, since r is below 2^23, so the low halves <c>VPMULLD</c> keeps are the scalar
        /// code's products.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<int> HighBits(Vector256<int> r, bool wide)
        {
            Vector256<int> r1 = Avx2.ShiftRightArithmetic(Avx2.Add(r, Vector256.Create(127)), 7);
            if (wide)
                return Avx2.And(Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.MultiplyLow(r1, Vector256.Create(1025)), Vector256.Create(1 << 21)), 22), Vector256.Create(15));

            r1 = Avx2.ShiftRightArithmetic(Avx2.Add(Avx2.MultiplyLow(r1, Vector256.Create(11275)), Vector256.Create(1 << 23)), 24);
            return Avx2.Xor(r1, Avx2.And(Avx2.ShiftRightArithmetic(Avx2.Subtract(Vector256.Create(43), r1), 31), r1));
        }

        /// <summary>
        /// Returns the largest of a vector's lanes.
        /// </summary>
        /// <param name="value">The lanes.</param>
        /// <returns>The largest lane, as a signed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MaxAcrossLanes(Vector256<int> value)
        {
            Vector128<int> maximum = Sse41.Max(value.GetLower(), value.GetUpper());
            maximum = Sse41.Max(maximum, Sse2.Shuffle(maximum, 0b01_00_11_10));
            maximum = Sse41.Max(maximum, Sse2.Shuffle(maximum, 0b10_11_00_01));
            return maximum.ToScalar();
        }

        /// <summary>
        /// Returns <see cref="MLDsaEngine.Canonicalize" /> of each lane: a value in [−q, q) mapped to [0, q).
        /// </summary>
        /// <param name="value">The coefficients, each at least −q and below q.</param>
        /// <returns>The canonical representatives.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<int> Canonicalize(Vector256<int> value) =>
            Avx2.Add(value, Avx2.And(Avx2.ShiftRightArithmetic(value, 31), Vector256.Create(Q)));

        /// <summary>
        /// Builds ζ · q⁻¹ mod 2^32 for every entry of the twiddle table.
        /// </summary>
        /// <returns>The 256-entry table.</returns>
        private static int[] BuildZetasTimesQInverse()
        {
            int[] table = new int[256];
            for (int m = 0; m < table.Length; m++)
                table[m] = unchecked(s_zetas[m] * QInverse);

            return table;
        }

        /// <summary>
        /// Builds the lane-ordered twiddles of the three layers the transforms rearrange coefficients for, for each of
        /// the sixteen groups of sixteen coefficients.
        /// </summary>
        /// <param name="forward">
        /// <see langword="true" /> for the forward transform's twiddles; <see langword="false" /> for the inverse's.
        /// </param>
        /// <returns>
        /// For each group, the layers of 4, 2 and 1 in that order, each as four vectors: the twiddles of the even
        /// lanes, those times q⁻¹, and the same two for the odd lanes, each value in the low 32 bits of its 64-bit
        /// pair.
        /// </returns>
        /// <remarks>
        /// The twiddles are those the scalar transforms use: ζ[k + b] for block b of a layer with k blocks going
        /// forward, and ζ[2k − 1 − b] going back. Lane l holds the coefficient the rearrangement of
        /// <see cref="LastLayers" /> puts there.
        /// </remarks>
        private static int[] BuildLaneTwiddles(bool forward)
        {
            int[] table = new int[16 * 3 * GroupTwiddleInts];
            Span<int> laneTwiddles = stackalloc int[Lanes];

            for (int group = 0; group < 16; group++)
            {
                for (int layer = 0; layer < 3; layer++)
                {
                    int length = 4 >> layer;
                    for (int lane = 0; lane < Lanes; lane++)
                    {
                        // The coefficient of the group in each lane of the low vector, for the layers of 4, 2 and 1.
                        int coefficient = layer switch
                        {
                            0 => lane < 4 ? lane : 4 + lane,
                            1 => ((lane >> 1) * 4) + (lane & 1),
                            _ => 2 * lane,
                        };

                        int blocks = N / (2 * length);
                        int block = ((16 * group) + coefficient) / (2 * length);
                        laneTwiddles[lane] = s_zetas[forward ? blocks + block : (2 * blocks) - 1 - block];
                    }

                    Span<int> destination = table.AsSpan(((group * 3) + layer) * GroupTwiddleInts, GroupTwiddleInts);
                    for (int lane = 0; lane < Lanes; lane++)
                    {
                        int even = laneTwiddles[lane & ~1];
                        int odd = laneTwiddles[lane | 1];
                        destination[lane] = even;
                        destination[Lanes + lane] = unchecked(even * QInverse);
                        destination[(2 * Lanes) + lane] = odd;
                        destination[(3 * Lanes) + lane] = unchecked(odd * QInverse);
                    }
                }
            }

            return table;
        }
    }
}
