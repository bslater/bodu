// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCore.SBoxes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

// The instruction sequences are Dag Arne Osvik's, from "Speeding up Serpent" (Third AES Candidate Conference, 2000), as
// Crypto++ publishes them in serpentp.h, which is in the public domain. Each circuit works in five registers; the
// assignments at its end return the outputs to their canonical positions, which the circuit leaves permuted. The
// vector kernels repeat the same sequences over vectors of words.
internal static partial class SerpentCore
{
    /// <summary>
    /// Applies the Serpent S-box S0 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of seventeen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void S0(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r3 ^= r0;
        r4 = r1;
        r1 &= r3;
        r4 ^= r2;
        r1 ^= r0;
        r0 |= r3;
        r0 ^= r4;
        r4 ^= r3;
        r3 ^= r2;
        r2 |= r1;
        r2 ^= r4;
        r4 = ~r4;
        r4 |= r1;
        r1 ^= r3;
        r1 ^= r4;
        r3 |= r0;
        r1 ^= r3;
        r4 ^= r3;

        x0 = r1;
        x1 = r4;
        x2 = r2;
        x3 = r0;
    }

    /// <summary>
    /// Applies the Serpent S-box S1 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of seventeen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void S1(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r0 = ~r0;
        r2 = ~r2;
        r4 = r0;
        r0 &= r1;
        r2 ^= r0;
        r0 |= r3;
        r3 ^= r2;
        r1 ^= r0;
        r0 ^= r4;
        r4 |= r1;
        r1 ^= r3;
        r2 |= r0;
        r2 &= r4;
        r0 ^= r1;
        r1 &= r2;
        r1 ^= r0;
        r0 &= r2;
        r0 ^= r4;

        x0 = r2;
        x1 = r0;
        x2 = r3;
        x3 = r1;
    }

    /// <summary>
    /// Applies the Serpent S-box S2 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of fourteen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void S2(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r4 = r0;
        r0 &= r2;
        r0 ^= r3;
        r2 ^= r1;
        r2 ^= r0;
        r3 |= r4;
        r3 ^= r1;
        r4 ^= r2;
        r1 = r3;
        r3 |= r4;
        r3 ^= r0;
        r0 &= r1;
        r4 ^= r0;
        r1 ^= r3;
        r1 ^= r4;
        r4 = ~r4;

        x0 = r2;
        x1 = r3;
        x2 = r1;
        x3 = r4;
    }

    /// <summary>
    /// Applies the Serpent S-box S3 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of seventeen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void S3(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r4 = r0;
        r0 |= r3;
        r3 ^= r1;
        r1 &= r4;
        r4 ^= r2;
        r2 ^= r3;
        r3 &= r0;
        r4 |= r1;
        r3 ^= r4;
        r0 ^= r1;
        r4 &= r0;
        r1 ^= r3;
        r4 ^= r2;
        r1 |= r0;
        r1 ^= r2;
        r0 ^= r3;
        r2 = r1;
        r1 |= r3;
        r1 ^= r0;

        x0 = r1;
        x1 = r2;
        x2 = r3;
        x3 = r4;
    }

    /// <summary>
    /// Applies the Serpent S-box S4 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of nineteen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void S4(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r1 ^= r3;
        r3 = ~r3;
        r2 ^= r3;
        r3 ^= r0;
        r4 = r1;
        r1 &= r3;
        r1 ^= r2;
        r4 ^= r3;
        r0 ^= r4;
        r2 &= r4;
        r2 ^= r0;
        r0 &= r1;
        r3 ^= r0;
        r4 |= r1;
        r4 ^= r0;
        r0 |= r3;
        r0 ^= r2;
        r2 &= r3;
        r0 = ~r0;
        r4 ^= r2;

        x0 = r1;
        x1 = r4;
        x2 = r0;
        x3 = r3;
    }

    /// <summary>
    /// Applies the Serpent S-box S5 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of eighteen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void S5(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r0 ^= r1;
        r1 ^= r3;
        r3 = ~r3;
        r4 = r1;
        r1 &= r0;
        r2 ^= r3;
        r1 ^= r2;
        r2 |= r4;
        r4 ^= r3;
        r3 &= r1;
        r3 ^= r0;
        r4 ^= r1;
        r4 ^= r2;
        r2 ^= r0;
        r0 &= r3;
        r2 = ~r2;
        r0 ^= r4;
        r4 |= r3;
        r2 ^= r4;

        x0 = r1;
        x1 = r3;
        x2 = r0;
        x3 = r2;
    }

    /// <summary>
    /// Applies the Serpent S-box S6 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of seventeen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void S6(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r2 = ~r2;
        r4 = r3;
        r3 &= r0;
        r0 ^= r4;
        r3 ^= r2;
        r2 |= r4;
        r1 ^= r3;
        r2 ^= r0;
        r0 |= r1;
        r2 ^= r1;
        r4 ^= r0;
        r0 |= r3;
        r0 ^= r2;
        r4 ^= r3;
        r4 ^= r0;
        r3 = ~r3;
        r2 &= r4;
        r2 ^= r3;

        x0 = r0;
        x1 = r1;
        x2 = r4;
        x3 = r2;
    }

    /// <summary>
    /// Applies the Serpent S-box S7 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of nineteen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void S7(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r4 = r2;
        r2 &= r1;
        r2 ^= r3;
        r3 &= r1;
        r4 ^= r2;
        r2 ^= r1;
        r1 ^= r0;
        r0 |= r4;
        r0 ^= r2;
        r3 ^= r1;
        r2 ^= r3;
        r3 &= r0;
        r3 ^= r4;
        r4 ^= r2;
        r2 &= r0;
        r4 = ~r4;
        r2 ^= r4;
        r4 &= r0;
        r1 ^= r3;
        r4 ^= r1;

        x0 = r2;
        x1 = r4;
        x2 = r3;
        x3 = r0;
    }

    /// <summary>
    /// Applies the inverse of the Serpent S-box S0 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of eighteen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void I0(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r2 = ~r2;
        r4 = r1;
        r1 |= r0;
        r4 = ~r4;
        r1 ^= r2;
        r2 |= r4;
        r1 ^= r3;
        r0 ^= r4;
        r2 ^= r0;
        r0 &= r3;
        r4 ^= r0;
        r0 |= r1;
        r0 ^= r2;
        r3 ^= r4;
        r2 ^= r1;
        r3 ^= r0;
        r3 ^= r1;
        r2 &= r3;
        r4 ^= r2;

        x0 = r0;
        x1 = r4;
        x2 = r1;
        x3 = r3;
    }

    /// <summary>
    /// Applies the inverse of the Serpent S-box S1 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of eighteen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void I1(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r4 = r1;
        r1 ^= r3;
        r3 &= r1;
        r4 ^= r2;
        r3 ^= r0;
        r0 |= r1;
        r2 ^= r3;
        r0 ^= r4;
        r0 |= r2;
        r1 ^= r3;
        r0 ^= r1;
        r1 |= r3;
        r1 ^= r0;
        r4 = ~r4;
        r4 ^= r1;
        r1 |= r0;
        r1 ^= r0;
        r1 |= r4;
        r3 ^= r1;

        x0 = r4;
        x1 = r0;
        x2 = r3;
        x3 = r2;
    }

    /// <summary>
    /// Applies the inverse of the Serpent S-box S2 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of eighteen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void I2(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r2 ^= r3;
        r3 ^= r0;
        r4 = r3;
        r3 &= r2;
        r3 ^= r1;
        r1 |= r2;
        r1 ^= r4;
        r4 &= r3;
        r2 ^= r3;
        r4 &= r0;
        r4 ^= r2;
        r2 &= r1;
        r2 |= r0;
        r3 = ~r3;
        r2 ^= r3;
        r0 ^= r3;
        r0 &= r1;
        r3 ^= r4;
        r3 ^= r0;

        x0 = r1;
        x1 = r4;
        x2 = r2;
        x3 = r3;
    }

    /// <summary>
    /// Applies the inverse of the Serpent S-box S3 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of seventeen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void I3(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r4 = r2;
        r2 ^= r1;
        r1 &= r2;
        r1 ^= r0;
        r0 &= r4;
        r4 ^= r3;
        r3 |= r1;
        r3 ^= r2;
        r0 ^= r4;
        r2 ^= r0;
        r0 |= r3;
        r0 ^= r1;
        r4 ^= r2;
        r2 &= r3;
        r1 |= r3;
        r1 ^= r2;
        r4 ^= r0;
        r2 ^= r4;

        x0 = r3;
        x1 = r0;
        x2 = r2;
        x3 = r1;
    }

    /// <summary>
    /// Applies the inverse of the Serpent S-box S4 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of nineteen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void I4(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r4 = r2;
        r2 &= r3;
        r2 ^= r1;
        r1 |= r3;
        r1 &= r0;
        r4 ^= r2;
        r4 ^= r1;
        r1 &= r2;
        r0 = ~r0;
        r3 ^= r4;
        r1 ^= r3;
        r3 &= r0;
        r3 ^= r2;
        r0 ^= r1;
        r2 &= r0;
        r3 ^= r0;
        r2 ^= r4;
        r2 |= r3;
        r3 ^= r0;
        r2 ^= r1;

        x0 = r0;
        x1 = r3;
        x2 = r2;
        x3 = r4;
    }

    /// <summary>
    /// Applies the inverse of the Serpent S-box S5 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of eighteen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void I5(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r1 = ~r1;
        r4 = r3;
        r2 ^= r1;
        r3 |= r0;
        r3 ^= r2;
        r2 |= r1;
        r2 &= r0;
        r4 ^= r3;
        r2 ^= r4;
        r4 |= r0;
        r4 ^= r1;
        r1 &= r2;
        r1 ^= r3;
        r4 ^= r2;
        r3 &= r4;
        r4 ^= r1;
        r3 ^= r0;
        r3 ^= r4;
        r4 = ~r4;

        x0 = r1;
        x1 = r4;
        x2 = r3;
        x3 = r2;
    }

    /// <summary>
    /// Applies the inverse of the Serpent S-box S6 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of sixteen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void I6(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r0 ^= r2;
        r4 = r2;
        r2 &= r0;
        r4 ^= r3;
        r2 = ~r2;
        r3 ^= r1;
        r2 ^= r3;
        r4 |= r0;
        r0 ^= r2;
        r3 ^= r4;
        r4 ^= r1;
        r1 &= r3;
        r1 ^= r0;
        r0 ^= r3;
        r0 |= r2;
        r3 ^= r1;
        r4 ^= r0;

        x0 = r1;
        x1 = r2;
        x2 = r4;
        x3 = r3;
    }

    /// <summary>
    /// Applies the inverse of the Serpent S-box S7 to four words in bitsliced form.
    /// </summary>
    /// <param name="x0">The first word, replaced by the first output word.</param>
    /// <param name="x1">The second word, replaced by the second output word.</param>
    /// <param name="x2">The third word, replaced by the third output word.</param>
    /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
    /// <remarks>
    /// Osvik's circuit of eighteen Boolean operations, free of branches and table lookups.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void I7(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint r0 = x0;
        uint r1 = x1;
        uint r2 = x2;
        uint r3 = x3;
        uint r4;

        r4 = r2;
        r2 ^= r0;
        r0 &= r3;
        r2 = ~r2;
        r4 |= r3;
        r3 ^= r1;
        r1 |= r0;
        r0 ^= r2;
        r2 &= r4;
        r1 ^= r2;
        r2 ^= r0;
        r0 |= r2;
        r3 &= r4;
        r0 ^= r3;
        r4 ^= r1;
        r3 ^= r4;
        r4 |= r0;
        r3 ^= r2;
        r4 ^= r2;

        x0 = r3;
        x1 = r0;
        x2 = r1;
        x3 = r4;
    }
}
