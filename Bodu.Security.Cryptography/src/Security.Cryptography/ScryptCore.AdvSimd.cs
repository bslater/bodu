// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCore.AdvSimd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class ScryptCore
{
    /// <summary>
    /// Supplies the lane rotations on ARM64, each a single <c>EXT</c> of the vector with itself.
    /// </summary>
    internal readonly struct AdvSimdIsa
        : IVector128Isa
    {
        /// <summary>
        /// Rotates the four 32-bit lanes one place toward lane 0.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLanes1(Vector128<uint> value) =>
            System.Runtime.Intrinsics.Arm.AdvSimd.ExtractVector128(value, value, 1);

        /// <summary>
        /// Rotates the four 32-bit lanes two places, swapping the vector's halves.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLanes2(Vector128<uint> value) =>
            System.Runtime.Intrinsics.Arm.AdvSimd.ExtractVector128(value, value, 2);

        /// <summary>
        /// Rotates the four 32-bit lanes three places toward lane 0.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLanes3(Vector128<uint> value) =>
            System.Runtime.Intrinsics.Arm.AdvSimd.ExtractVector128(value, value, 3);
    }
}
