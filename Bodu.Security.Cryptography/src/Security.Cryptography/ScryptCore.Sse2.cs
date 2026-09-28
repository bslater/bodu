// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCore.Sse2.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class ScryptCore
{
    /// <summary>
    /// Supplies the lane rotations on x64, each a single <c>PSHUFD</c>.
    /// </summary>
    internal readonly struct Sse2Isa
        : IVector128Isa
    {
        /// <summary>
        /// Rotates the four 32-bit lanes one place toward lane 0.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLanes1(Vector128<uint> value) =>
            System.Runtime.Intrinsics.X86.Sse2.Shuffle(value, 0b00_11_10_01);

        /// <summary>
        /// Rotates the four 32-bit lanes two places, swapping the vector's halves.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLanes2(Vector128<uint> value) =>
            System.Runtime.Intrinsics.X86.Sse2.Shuffle(value, 0b01_00_11_10);

        /// <summary>
        /// Rotates the four 32-bit lanes three places toward lane 0.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLanes3(Vector128<uint> value) =>
            System.Runtime.Intrinsics.X86.Sse2.Shuffle(value, 0b10_01_00_11);
    }
}
