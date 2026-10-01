// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCore.SBox1.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

internal static partial class SerpentCore
{
    /// <summary>
    /// Supplies S-box S1 and its inverse to the wide-block rounds.
    /// </summary>
    internal readonly struct SBox1
        : IRoundSBox
    {
        /// <summary>
        /// Applies S1 to four words in bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Forward(ref uint x0, ref uint x1, ref uint x2, ref uint x3) =>
            S1(ref x0, ref x1, ref x2, ref x3);

        /// <summary>
        /// Applies the inverse of S1 to four words in bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Inverse(ref uint x0, ref uint x1, ref uint x2, ref uint x3) =>
            I1(ref x0, ref x1, ref x2, ref x3);
    }
}
