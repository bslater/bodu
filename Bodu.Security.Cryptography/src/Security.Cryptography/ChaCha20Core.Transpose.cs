// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Core.Transpose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace Bodu.Security.Cryptography;

internal static partial class ChaCha20Core
{
    /// <summary>
    /// Transposes four rows of four 32-bit words in place, so that row <c>i</c> becomes column <c>i</c>: with SSE2
    /// unpacks on x86 and x64, and with zips on ARM64.
    /// </summary>
    /// <param name="row0">The first row, replaced by the first column.</param>
    /// <param name="row1">The second row, replaced by the second column.</param>
    /// <param name="row2">The third row, replaced by the third column.</param>
    /// <param name="row3">The fourth row, replaced by the fourth column.</param>
    /// <remarks>
    /// The 128-bit kernels of <see cref="ChaCha20Core" />, <see cref="Salsa20Core" /> and <see cref="SerpentCore" />
    /// share this step. Its instructions depend on the architecture alone, not on the instruction set a kernel rotates
    /// with, so they are chosen here, and the choice folds away when the JIT compiles the kernel.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Transpose(ref Vector128<uint> row0, ref Vector128<uint> row1, ref Vector128<uint> row2, ref Vector128<uint> row3)
    {
        if (AdvSimd.Arm64.IsSupported)
        {
            Blake2sCore.AdvSimdIsa.Transpose(ref row0, ref row1, ref row2, ref row3);
        }
        else
        {
            Blake2sCore.Ssse3Isa.Transpose(ref row0, ref row1, ref row2, ref row3);
        }
    }
}
