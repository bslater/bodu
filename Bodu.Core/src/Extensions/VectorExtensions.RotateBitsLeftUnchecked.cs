// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VectorExtensions.RotateBitsLeftUnchecked.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Bodu.Extensions;

internal static partial class VectorExtensions
{
    /// <summary>
    /// Rotates every 32-bit lane of the specified 128-bit vector left by the specified number of bits, with the
    /// instructions of <typeparamref name="TIsa" /> and without validating <paramref name="count" />.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set that performs the rotation.</typeparam>
    /// <param name="value">The vector whose lanes are to be rotated.</param>
    /// <param name="count">The number of bits to rotate each lane by.</param>
    /// <returns>
    /// A vector whose lanes hold the lanes of <paramref name="value" />, each rotated left by <paramref name="count" />
    /// bits.
    /// </returns>
    /// <remarks>
    /// The vector counterpart of <see cref="NumericExtensions.RotateBitsLeftUnchecked(uint, int)" />.
    /// <paramref name="count" /> must be a constant from 1 to 31, which lets the JIT fold the choice of instructions
    /// when it inlines the call, and the processor must support <typeparamref name="TIsa" />; neither is checked.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<uint> RotateBitsLeftUnchecked<TIsa>(this Vector128<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count)
        where TIsa : struct, IVector128Rotation =>
        TIsa.RotateLeft(value, count);

    /// <summary>
    /// Rotates every 32-bit lane of the specified 256-bit vector left by the specified number of bits, with the
    /// instructions of <typeparamref name="TIsa" /> and without validating <paramref name="count" />.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set that performs the rotation.</typeparam>
    /// <param name="value">The vector whose lanes are to be rotated.</param>
    /// <param name="count">The number of bits to rotate each lane by.</param>
    /// <returns>
    /// A vector whose lanes hold the lanes of <paramref name="value" />, each rotated left by <paramref name="count" />
    /// bits.
    /// </returns>
    /// <remarks>
    /// The vector counterpart of <see cref="NumericExtensions.RotateBitsLeftUnchecked(uint, int)" />.
    /// <paramref name="count" /> must be a constant from 1 to 31, which lets the JIT fold the choice of instructions
    /// when it inlines the call, and the processor must support <typeparamref name="TIsa" />; neither is checked.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<uint> RotateBitsLeftUnchecked<TIsa>(this Vector256<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count)
        where TIsa : struct, IVector256Rotation =>
        TIsa.RotateLeft(value, count);

    /// <summary>
    /// Rotates every 64-bit lane of the specified 256-bit vector left by the specified number of bits, with the
    /// instructions of <typeparamref name="TIsa" /> and without validating <paramref name="count" />.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set that performs the rotation.</typeparam>
    /// <param name="value">The vector whose lanes are to be rotated.</param>
    /// <param name="count">The number of bits to rotate each lane by.</param>
    /// <returns>
    /// A vector whose lanes hold the lanes of <paramref name="value" />, each rotated left by <paramref name="count" />
    /// bits.
    /// </returns>
    /// <remarks>
    /// The vector counterpart of <see cref="NumericExtensions.RotateBitsLeftUnchecked(ulong, int)" />.
    /// <paramref name="count" /> must be a constant from 1 to 63, which lets the JIT fold the choice of instructions
    /// when it inlines the call, and the processor must support <typeparamref name="TIsa" />; neither is checked.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<ulong> RotateBitsLeftUnchecked<TIsa>(this Vector256<ulong> value, [ConstantExpected(Min = 1, Max = 63)] byte count)
        where TIsa : struct, IVector256Rotation =>
        TIsa.RotateLeft(value, count);

    /// <summary>
    /// Rotates every 32-bit lane of the specified 512-bit vector left by the specified number of bits, with the
    /// instructions of <typeparamref name="TIsa" /> and without validating <paramref name="count" />.
    /// </summary>
    /// <typeparam name="TIsa">The instruction set that performs the rotation.</typeparam>
    /// <param name="value">The vector whose lanes are to be rotated.</param>
    /// <param name="count">The number of bits to rotate each lane by.</param>
    /// <returns>
    /// A vector whose lanes hold the lanes of <paramref name="value" />, each rotated left by <paramref name="count" />
    /// bits.
    /// </returns>
    /// <remarks>
    /// The vector counterpart of <see cref="NumericExtensions.RotateBitsLeftUnchecked(uint, int)" />.
    /// <paramref name="count" /> must be a constant from 1 to 31, which lets the JIT fold the choice of instructions
    /// when it inlines the call, and the processor must support <typeparamref name="TIsa" />; neither is checked.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector512<uint> RotateBitsLeftUnchecked<TIsa>(this Vector512<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count)
        where TIsa : struct, IVector512Rotation =>
        TIsa.RotateLeft(value, count);
}
