// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IVector128Rotation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics;

namespace Bodu.Extensions;

/// <summary>
/// Defines how one instruction set rotates the 32-bit lanes of a 128-bit vector, for
/// <see cref="VectorExtensions.RotateBitsLeftUnchecked{TIsa}(Vector128{uint}, byte)" />.
/// </summary>
/// <remarks>
/// Implementations are structs supplied as type arguments, so code that is generic over one is compiled once per
/// instruction set with the rotation inlined. <see cref="VectorRotation" /> holds the implementations.
/// </remarks>
internal interface IVector128Rotation
{
    /// <summary>
    /// Rotates every 32-bit lane of a vector left by a number of bits.
    /// </summary>
    /// <param name="value">The lanes to rotate.</param>
    /// <param name="count">The number of bits to rotate each lane by.</param>
    /// <returns>The rotated lanes.</returns>
    /// <remarks>
    /// <paramref name="count" /> must be a constant from 1 to 31; it is not validated.
    /// </remarks>
    static abstract Vector128<uint> RotateLeft(Vector128<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count);
}
