// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the BLAKE2b compression function <c>F</c> (RFC 7693, Section 3.2), shared by <see cref="Blake2b" /> and
/// Argon2's <see cref="Argon2Blake2b" />.
/// </summary>
/// <remarks>
/// <para>
/// Each kernel compresses one 128-byte block into the eight-word chaining state. Dispatch picks the widest kernel the
/// processor supports and the process allows; every kernel produces the same state.
/// </para>
/// <para>
/// The kernels read the message words straight from the block and keep the working vector in registers, so they leave
/// no copy of the message or of the working vector in memory of their own. Values the JIT spills to its own stack slots
/// are beyond the library's reach.
/// </para>
/// </remarks>
internal static partial class Blake2bCore
{
    /// <summary>The number of bytes in a BLAKE2b block.</summary>
    internal const int BlockBytes = 128;

    /// <summary>The number of 64-bit words in the BLAKE2b chaining state.</summary>
    internal const int StateWords = 8;

    /// <summary>The first word of the BLAKE2b initialization vector (the SHA-512 IV, RFC 7693, Section 2.6).</summary>
    private const ulong Iv0 = 0x6A09E667F3BCC908UL;

    /// <summary>The second word of the BLAKE2b initialization vector.</summary>
    private const ulong Iv1 = 0xBB67AE8584CAA73BUL;

    /// <summary>The third word of the BLAKE2b initialization vector.</summary>
    private const ulong Iv2 = 0x3C6EF372FE94F82BUL;

    /// <summary>The fourth word of the BLAKE2b initialization vector.</summary>
    private const ulong Iv3 = 0xA54FF53A5F1D36F1UL;

    /// <summary>The fifth word of the BLAKE2b initialization vector.</summary>
    private const ulong Iv4 = 0x510E527FADE682D1UL;

    /// <summary>The sixth word of the BLAKE2b initialization vector.</summary>
    private const ulong Iv5 = 0x9B05688C2B3E6C1FUL;

    /// <summary>The seventh word of the BLAKE2b initialization vector.</summary>
    private const ulong Iv6 = 0x1F83D9ABFB41BD6BUL;

    /// <summary>The eighth word of the BLAKE2b initialization vector.</summary>
    private const ulong Iv7 = 0x5BE0CD19137E2179UL;

    /// <summary>
    /// Gets the BLAKE2b initialization vector: the eight words the chaining state starts from before the parameter
    /// block is folded in.
    /// </summary>
    internal static ReadOnlySpan<ulong> InitializationVector => [Iv0, Iv1, Iv2, Iv3, Iv4, Iv5, Iv6, Iv7];

    /// <summary>
    /// Compresses one block into the chaining state with the kernel dispatch selects.
    /// </summary>
    /// <param name="state">The eight-word chaining state, updated in place.</param>
    /// <param name="block">The 128-byte block.</param>
    /// <param name="counter">The number of message bytes compressed so far, this block included.</param>
    /// <param name="last"><see langword="true" /> for the final block, which sets the finalization flag.</param>
    internal static void Compress(Span<ulong> state, ReadOnlySpan<byte> block, ulong counter, bool last) =>
        Compress(SelectKernel(), state, block, counter, last);

    /// <summary>
    /// Compresses one block into the chaining state with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="state">The eight-word chaining state, updated in place.</param>
    /// <param name="block">The 128-byte block.</param>
    /// <param name="counter">The number of message bytes compressed so far, this block included.</param>
    /// <param name="last"><see langword="true" /> for the final block, which sets the finalization flag.</param>
    internal static void Compress(KernelKind kernel, Span<ulong> state, ReadOnlySpan<byte> block, ulong counter, bool last)
    {
        ref ulong h = ref MemoryMarshal.GetReference(state);
        ref byte m = ref MemoryMarshal.GetReference(block);
        ulong finalization = last ? ulong.MaxValue : 0;

        switch (kernel == KernelKind.Auto ? SelectKernel() : kernel)
        {
            case KernelKind.Avx512:
                Vector256Kernel<Avx512Isa>.Compress(ref h, ref m, counter, finalization);
                break;

            case KernelKind.Avx2:
                Vector256Kernel<Avx2Isa>.Compress(ref h, ref m, counter, finalization);
                break;

            default:
                CompressScalar(ref h, ref m, counter, finalization);
                break;
        }
    }

    /// <summary>
    /// Selects the widest kernel the processor supports and the process allows: AVX-512, then AVX2, then the scalar
    /// kernel.
    /// </summary>
    /// <returns>The kernel dispatch runs; never <see cref="KernelKind.Auto" />.</returns>
    /// <remarks>
    /// Every gate honors the <see cref="SimdCapabilities.DisableSimdSwitchName" /> switch, which pins the scalar
    /// kernel.
    /// </remarks>
    internal static KernelKind SelectKernel()
    {
        if (SimdCapabilities.Avx512FVL)
            return KernelKind.Avx512;

        return SimdCapabilities.Avx2 ? KernelKind.Avx2 : KernelKind.Scalar;
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
        KernelKind.Avx2 => System.Runtime.Intrinsics.X86.Avx2.IsSupported,
        KernelKind.Avx512 => System.Runtime.Intrinsics.X86.Avx2.IsSupported && System.Runtime.Intrinsics.X86.Avx512F.VL.IsSupported,
        _ => false,
    };
}
