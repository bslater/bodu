// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ghash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes GHASH (NIST SP 800-38D) and POLYVAL (RFC 8452) over runs of blocks, with the hash key prepared once so that
/// the carry-less kernels fold four blocks into each reduction.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Key" /> fixes the function - GHASH or POLYVAL - the kernel, and the key-dependent values.
/// <see cref="Update" /> folds data into a 16-byte state held in the function's own byte order and pads a final partial
/// block with zeros, so the separately padded associated data and text of GCM and GCM-SIV are two calls.
/// </para>
/// <para>
/// Three kernels compute the same function: the x64 carry-less multiply (<c>PCLMULQDQ</c>) and the ARM64 polynomial
/// multiply (<c>PMULL</c>), both through the <see cref="IClmulIsa" /> shim, and a portable scalar multiply built from
/// integer multiplications whose carries are masked away. None branches on, or indexes memory by, the key or the data.
/// </para>
/// <para>
/// POLYVAL is computed as GHASH through RFC 8452 Appendix A's isomorphism,
/// <c>POLYVAL(H, X) = ByteReverse(GHASH(mulX_GHASH(ByteReverse(H)), ByteReverse(X)))</c>. The carry-less kernels
/// already work on byte-reversed GHASH blocks, so for POLYVAL they take the blocks as they are; the scalar kernel reads
/// them with the opposite byte order.
/// </para>
/// </remarks>
internal static partial class Ghash
{
    /// <summary>The GHASH and POLYVAL block size, in bytes.</summary>
    internal const int BlockBytes = 16;

    /// <summary>
    /// Folds <paramref name="data" /> into <paramref name="state" />, one block at a time in effect, padding a final
    /// partial block with zeros.
    /// </summary>
    /// <param name="key">The prepared key, which also selects the function and the kernel.</param>
    /// <param name="state">The 16-byte running state, in the function's byte order; updated in place.</param>
    /// <param name="data">The data to fold in.</param>
    /// <exception cref="ArgumentException"><paramref name="state" /> is not 16 bytes.</exception>
    internal static void Update(in Key key, Span<byte> state, ReadOnlySpan<byte> data)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(state, BlockBytes);

        switch (key.Kernel)
        {
            case KernelKind.Pclmulqdq:
                UpdateClmul<PclmulqdqIsa>(in key, state, data);
                break;

            case KernelKind.Pmull:
                UpdateClmul<PmullIsa>(in key, state, data);
                break;

            default:
                UpdateScalar(in key, state, data);
                break;
        }
    }

    /// <summary>
    /// Selects the kernel for this process: the carry-less multiply of whichever architecture it runs on, or the scalar
    /// kernel when that is unavailable or the SIMD switch is set.
    /// </summary>
    /// <returns>The kernel to prepare keys for.</returns>
    internal static KernelKind SelectKernel() =>
        SimdCapabilities.Pclmulqdq ? KernelKind.Pclmulqdq : SimdCapabilities.Pmull ? KernelKind.Pmull : KernelKind.Scalar;

    /// <summary>
    /// Returns a value indicating whether the processor can run <paramref name="kernel" />, whatever the SIMD switch
    /// says.
    /// </summary>
    /// <param name="kernel">The kernel to check.</param>
    /// <returns><see langword="true" /> if the processor has the kernel's instructions.</returns>
    internal static bool IsSupported(KernelKind kernel) => kernel switch
    {
        KernelKind.Scalar => true,
        KernelKind.Pclmulqdq => System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported && System.Runtime.Intrinsics.X86.Ssse3.IsSupported,
        KernelKind.Pmull => System.Runtime.Intrinsics.Arm.Aes.IsSupported && System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported,
        _ => false,
    };
}
