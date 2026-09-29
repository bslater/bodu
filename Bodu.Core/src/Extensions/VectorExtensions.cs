// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VectorExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides bit-level operations on every lane of a hardware vector: the vector counterparts of the unchecked rotations
/// in <see cref="NumericExtensions" />.
/// </summary>
/// <remarks>
/// <para>
/// Each operation takes the instruction set that performs it as a type argument, one of the implementations in
/// <see cref="VectorRotation" />, rather than using the best one the processor offers. A kernel that is generic over
/// the instruction set can then be compiled, and tested, once per instruction set on a machine that supports several.
/// </para>
/// <para>
/// The operations validate nothing. They are intended for performance-critical, trusted callers such as the SIMD
/// kernels in <c>Bodu.Security.Cryptography</c>, which pass constant bit counts and check that the processor supports
/// the instruction set before running a kernel.
/// </para>
/// </remarks>
internal static partial class VectorExtensions
{
}
