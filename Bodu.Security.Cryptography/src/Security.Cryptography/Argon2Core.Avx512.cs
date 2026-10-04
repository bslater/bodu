// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.Avx512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Core
{
    /// <summary>
    /// Supplies the 256-bit kernel's rotation by 63 bits with AVX-512VL: a single <c>VPRORQ</c> in place of
    /// <see cref="Avx2Isa" />'s addition, shift and XOR.
    /// </summary>
    internal readonly struct Avx512Isa
        : IVector256Isa
    {
        /// <summary>
        /// Rotates each word right by 63 bits, with <c>VPRORQ</c>.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight63(Vector256<ulong> x) =>
            Avx512F.VL.RotateRight(x, 63);
    }
}
