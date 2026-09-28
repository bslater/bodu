// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCore.Vector128Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class ScryptCore
{
    /// <summary>
    /// Supplies the three lane rotations of the 128-bit Salsa20/8 kernel, which have no portable
    /// <see cref="Vector128" /> form that compiles to a single instruction, for one instruction set.
    /// </summary>
    /// <remarks>
    /// <see cref="Vector128Kernel{TIsa}" /> is written once against this interface and specialized per instruction set,
    /// so the kernel's logic is shared between x64 and ARM64 and only these members differ. Each member is a fixed
    /// permutation of its operand's lanes.
    /// </remarks>
    internal interface IVector128Isa
    {
        /// <summary>
        /// Rotates the four 32-bit lanes one place toward lane 0: lane <c>i</c> of the result is lane
        /// <c>(i + 1) mod 4</c> of <paramref name="value" />.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        static abstract Vector128<uint> RotateLanes1(Vector128<uint> value);

        /// <summary>
        /// Rotates the four 32-bit lanes two places, swapping the vector's halves.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        static abstract Vector128<uint> RotateLanes2(Vector128<uint> value);

        /// <summary>
        /// Rotates the four 32-bit lanes three places toward lane 0: lane <c>i</c> of the result is lane
        /// <c>(i + 3) mod 4</c> of <paramref name="value" />.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        static abstract Vector128<uint> RotateLanes3(Vector128<uint> value);
    }
}
