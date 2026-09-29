// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VectorRotation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides the instruction-set implementations of the vector rotations in <see cref="VectorExtensions" />: one struct
/// per instruction set, supplied as the <c>TIsa</c> type argument.
/// </summary>
/// <remarks>
/// <para>
/// Code that is generic over an implementation is compiled once per instruction set, with that set's instructions
/// inlined. A caller therefore chooses the instruction set once, outside its hot loop, and a test can run every
/// implementation the processor supports rather than only the best one.
/// </para>
/// <para>
/// Each implementation requires its instruction set, and the caller checks for it before running one:
/// <see cref="Ssse3" /> requires SSSE3, <see cref="AdvSimd" /> ARM64 AdvSimd, <see cref="Avx2" /> AVX2, and
/// <see cref="Avx512" /> AVX-512F, with AVX-512VL for its 128-bit and 256-bit rotations.
/// </para>
/// </remarks>
internal static partial class VectorRotation
{
}
