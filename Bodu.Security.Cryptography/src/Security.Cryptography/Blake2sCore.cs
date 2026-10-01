// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the BLAKE2s compression function <c>F</c> (RFC 7693, Section 3.2) for <see cref="Blake2s" />.
/// </summary>
/// <remarks>
/// <para>
/// Each kernel compresses one 64-byte block into the eight-word chaining state. Dispatch picks the widest kernel the
/// processor supports and the process allows; every kernel produces the same state.
/// </para>
/// <para>
/// The kernels read the message words straight from the block and keep the working vector in registers, so they leave
/// no copy of the message or of the working vector in memory of their own. Values the JIT spills to its own stack slots
/// are beyond the library's reach.
/// </para>
/// <para>
/// Every kernel forbids inlining, so it is compiled on its own with its own inlining budget. Under .NET 8's dynamic PGO
/// the dispatcher otherwise inlined whichever kernel it found hot, ran out of budget inside it, and left the kernel's
/// own helpers as calls.
/// </para>
/// </remarks>
internal static partial class Blake2sCore
{
    /// <summary>The number of bytes in a BLAKE2s block.</summary>
    internal const int BlockBytes = 64;

    /// <summary>The number of 32-bit words in the BLAKE2s chaining state.</summary>
    internal const int StateWords = 8;

    /// <summary>The first word of the BLAKE2s initialization vector (the SHA-256 IV, RFC 7693, Section 2.6).</summary>
    private const uint Iv0 = 0x6A09E667U;

    /// <summary>The second word of the BLAKE2s initialization vector.</summary>
    private const uint Iv1 = 0xBB67AE85U;

    /// <summary>The third word of the BLAKE2s initialization vector.</summary>
    private const uint Iv2 = 0x3C6EF372U;

    /// <summary>The fourth word of the BLAKE2s initialization vector.</summary>
    private const uint Iv3 = 0xA54FF53AU;

    /// <summary>The fifth word of the BLAKE2s initialization vector.</summary>
    private const uint Iv4 = 0x510E527FU;

    /// <summary>The sixth word of the BLAKE2s initialization vector.</summary>
    private const uint Iv5 = 0x9B05688CU;

    /// <summary>The seventh word of the BLAKE2s initialization vector.</summary>
    private const uint Iv6 = 0x1F83D9ABU;

    /// <summary>The eighth word of the BLAKE2s initialization vector.</summary>
    private const uint Iv7 = 0x5BE0CD19U;

    /// <summary>
    /// Gets the BLAKE2s message schedule σ for all ten rounds (RFC 7693, Section 2.7): sixteen message-word indices per
    /// round.
    /// </summary>
    /// <remarks>
    /// The scalar kernel writes its rounds out with these indices as constants; the 128-bit kernel reads them from
    /// here.
    /// </remarks>
    internal static ReadOnlySpan<byte> Sigma =>
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
        14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3,
        11, 8, 12, 0, 5, 2, 15, 13, 10, 14, 3, 6, 7, 1, 9, 4,
        7, 9, 3, 1, 13, 12, 11, 14, 2, 6, 5, 10, 4, 0, 15, 8,
        9, 0, 5, 7, 2, 4, 10, 15, 14, 1, 11, 12, 6, 8, 3, 13,
        2, 12, 6, 10, 0, 11, 8, 3, 4, 13, 7, 5, 15, 14, 1, 9,
        12, 5, 1, 15, 14, 13, 4, 10, 0, 7, 6, 3, 9, 2, 8, 11,
        13, 11, 7, 14, 12, 1, 3, 9, 5, 0, 15, 4, 8, 6, 2, 10,
        6, 15, 14, 9, 11, 3, 0, 8, 12, 2, 13, 7, 1, 4, 10, 5,
        10, 2, 8, 4, 7, 6, 1, 5, 15, 11, 9, 14, 3, 12, 13, 0,
    ];

    /// <summary>
    /// Gets the BLAKE2s initialization vector: the eight words the chaining state starts from before the parameter
    /// block is folded in.
    /// </summary>
    internal static ReadOnlySpan<uint> InitializationVector => [Iv0, Iv1, Iv2, Iv3, Iv4, Iv5, Iv6, Iv7];

    /// <summary>
    /// Compresses one block into the chaining state with the kernel dispatch selects.
    /// </summary>
    /// <param name="state">The eight-word chaining state, updated in place.</param>
    /// <param name="block">The 64-byte block.</param>
    /// <param name="counter">The number of message bytes compressed so far, this block included.</param>
    /// <param name="last"><see langword="true" /> for the final block, which sets the finalization flag.</param>
    internal static void Compress(Span<uint> state, ReadOnlySpan<byte> block, ulong counter, bool last) =>
        Compress(SelectKernel(), state, block, counter, last);

    /// <summary>
    /// Compresses one block into the chaining state with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="state">The eight-word chaining state, updated in place.</param>
    /// <param name="block">The 64-byte block.</param>
    /// <param name="counter">The number of message bytes compressed so far, this block included.</param>
    /// <param name="last"><see langword="true" /> for the final block, which sets the finalization flag.</param>
    internal static void Compress(KernelKind kernel, Span<uint> state, ReadOnlySpan<byte> block, ulong counter, bool last)
    {
        ref uint h = ref MemoryMarshal.GetReference(state);
        ref byte m = ref MemoryMarshal.GetReference(block);
        uint finalization = last ? uint.MaxValue : 0;

        switch (kernel == KernelKind.Auto ? SelectKernel() : kernel)
        {
            case KernelKind.Avx512:
                Vector128Kernel<Avx512Isa>.Compress(ref h, ref m, counter, finalization);
                break;

            case KernelKind.AdvSimd:
                Vector128Kernel<AdvSimdIsa>.Compress(ref h, ref m, counter, finalization);
                break;

            case KernelKind.Ssse3:
                Vector128Kernel<Ssse3Isa>.Compress(ref h, ref m, counter, finalization);
                break;

            default:
                CompressScalar(ref h, ref m, counter, finalization);
                break;
        }
    }

    /// <summary>
    /// Selects the kernel dispatch runs: on x64 the widest the processor supports and the process allows, AVX-512, then
    /// SSSE3; the scalar kernel on ARM64 and everywhere else.
    /// </summary>
    /// <returns>The kernel dispatch runs; never <see cref="KernelKind.Auto" />.</returns>
    /// <remarks>
    /// <para>
    /// Every gate honors the <see cref="SimdCapabilities.DisableSimdSwitchName" /> switch, which pins the scalar
    /// kernel.
    /// </para>
    /// <para>
    /// The AdvSimd kernel waits on <see cref="SimdCapabilities.AdvSimdSingleState" />, which is closed: on the ARM64
    /// processors measured, the scalar kernel ran faster.
    /// </para>
    /// </remarks>
    internal static KernelKind SelectKernel()
    {
        if (SimdCapabilities.Avx512FVL)
            return KernelKind.Avx512;

        if (SimdCapabilities.AdvSimdSingleState)
            return KernelKind.AdvSimd;

        return SimdCapabilities.Ssse3 ? KernelKind.Ssse3 : KernelKind.Scalar;
    }

    /// <summary>
    /// Determines whether the processor can run the specified kernel, whether or not the process allows vector code.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <returns>
    /// <see langword="true" /> if the processor supports every instruction the kernel uses; otherwise,
    /// <see langword="false" />.
    /// </returns>
    internal static bool IsSupported(KernelKind kernel) => kernel switch
    {
        KernelKind.Auto or KernelKind.Scalar => true,
        KernelKind.Ssse3 => System.Runtime.Intrinsics.X86.Ssse3.IsSupported,
        KernelKind.AdvSimd => System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported,
        KernelKind.Avx512 => System.Runtime.Intrinsics.X86.Ssse3.IsSupported && System.Runtime.Intrinsics.X86.Avx512F.VL.IsSupported,
        _ => false,
    };
}
