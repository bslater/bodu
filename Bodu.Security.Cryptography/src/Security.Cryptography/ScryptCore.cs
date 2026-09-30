// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the scrypt sequential memory-hard function defined by RFC 7914 - the PBKDF2-HMAC-SHA256 envelope, the
/// <c>scryptROMix</c> and <c>scryptBlockMix</c> mixing functions, and the Salsa20/8 core.
/// </summary>
/// <remarks>
/// <para>
/// ROMix writes each link of its chain straight into <c>V</c>, and BlockMix writes each Salsa20/8 result straight into
/// its place in the output, so no 64-byte block is copied on its way through. The second loop folds <c>X xor V[j]</c>
/// into BlockMix's reads instead of writing it out first.
/// </para>
/// <para>
/// ROMix is generic over an <see cref="IScryptKernel" />, BlockMix for one instruction set, so each kernel runs in a
/// loop the JIT specializes for it: the scalar kernel everywhere, and a 128-bit kernel over SSE2 on x64 or AdvSimd on
/// ARM64. Every kernel produces the same key.
/// </para>
/// <para>
/// <c>V</c> and the ROMix scratch live in a <see cref="Workspace" /> in native memory, taken from the
/// <see cref="NativeBufferPool" /> Argon2 also uses, so a derivation neither allocates them on the collected heap nor
/// provokes a gen2 collection.
/// </para>
/// <para>
/// Every buffer that holds a password-derived word - <c>B</c>, <c>V</c>, and the ROMix scratch - is cleared before it
/// is released. Values the JIT keeps in registers or spills to its own stack slots are beyond the library's reach.
/// </para>
/// </remarks>
[SkipLocalsInit]
internal static partial class ScryptCore
{
    /// <summary>The number of 32-bit words in a Salsa20 block (64 bytes).</summary>
    private const int BlockWords = 16;

    /// <summary>
    /// Derives <paramref name="output" />.Length bytes from the password and salt using scrypt with the given cost
    /// parameters.
    /// </summary>
    /// <param name="password">The password <c>P</c>.</param>
    /// <param name="salt">The salt <c>S</c>.</param>
    /// <param name="costN">The CPU/memory cost parameter <c>N</c> (a power of two greater than one).</param>
    /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
    /// <param name="parallelization">The parallelization parameter <c>p</c>.</param>
    /// <param name="output">The destination buffer; its length is the derived-key length <c>dkLen</c>.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The greatest number of threads the derivation may use, the calling thread included; <c>-1</c> for up to one per
    /// processor.
    /// </param>
    /// <exception cref="CryptographicException">
    /// <c>B</c> cannot be represented as a single managed array, or <c>V</c> as a single span.
    /// </exception>
    /// <exception cref="OutOfMemoryException"><c>V</c> cannot be allocated.</exception>
    internal static void DeriveKey(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        int costN,
        int blockSizeR,
        int parallelization,
        Span<byte> output,
        int maxDegreeOfParallelism) =>
        DeriveKey(password, salt, costN, blockSizeR, parallelization, output, new MixOptions(maxDegreeOfParallelism));

    /// <summary>
    /// Derives <paramref name="output" />.Length bytes from the password and salt using scrypt with the given cost
    /// parameters, running the ROMix units as <paramref name="options" /> describe.
    /// </summary>
    /// <param name="password">The password <c>P</c>.</param>
    /// <param name="salt">The salt <c>S</c>.</param>
    /// <param name="costN">The CPU/memory cost parameter <c>N</c> (a power of two greater than one).</param>
    /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
    /// <param name="parallelization">The parallelization parameter <c>p</c>.</param>
    /// <param name="output">The destination buffer; its length is the derived-key length <c>dkLen</c>.</param>
    /// <param name="options">How the ROMix units run.</param>
    /// <exception cref="CryptographicException">
    /// <c>B</c> cannot be represented as a single managed array, or <c>V</c> as a single span.
    /// </exception>
    /// <exception cref="OutOfMemoryException"><c>V</c> cannot be allocated.</exception>
    internal static void DeriveKey(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        int costN,
        int blockSizeR,
        int parallelization,
        Span<byte> output,
        in MixOptions options)
    {
        int unitWords = 2 * blockSizeR * BlockWords;   // 32*r words = 128*r bytes per ROMix unit
        long totalBytes = (long)parallelization * unitWords * sizeof(uint);
        long workspaceWords = ((long)costN + 1) * unitWords;
        if (totalBytes > Array.MaxLength || workspaceWords > int.MaxValue)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_KdfMemoryExceedsLimit);

        byte[] rented = ArrayPool<byte>.Shared.Rent((int)totalBytes);
        Span<byte> b = rented.AsSpan(0, (int)totalBytes);

        try
        {
            // Step 1: B = PBKDF2-HMAC-SHA256(P, S, 1, p * 128 * r).
            Rfc2898DeriveBytes.Pbkdf2(password, salt, b, iterations: 1, HashAlgorithmName.SHA256);

            // B's bytes are its little-endian words, so the words are B itself on a little-endian processor.
            Span<uint> words = MemoryMarshal.Cast<byte, uint>(b);
            if (!BitConverter.IsLittleEndian)
                BinaryPrimitives.ReverseEndianness(words, words);

            // Step 2: B_i = scryptROMix(r, B_i, N) for each of the p independent blocks.
            KernelKind kernel = options.ResolveKernel();
            int workers = options.ResolveWorkers(parallelization, (long)costN * unitWords * sizeof(uint));
            if (workers > 1)
                MixInParallel(rented, (int)totalBytes, costN, blockSizeR, parallelization, workers, kernel, options.Pool);
            else
                Mix(words, costN, blockSizeR, kernel, options.Pool);

            if (!BitConverter.IsLittleEndian)
                BinaryPrimitives.ReverseEndianness(words, words);

            // Step 3: DK = PBKDF2-HMAC-SHA256(P, B, 1, dkLen).
            Rfc2898DeriveBytes.Pbkdf2(password, b, output, iterations: 1, HashAlgorithmName.SHA256);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(b);
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Applies ROMix to every unit of <c>B</c> on the calling thread, with one workspace reused across the units.
    /// </summary>
    /// <param name="words">The <c>p</c> units of <c>B</c>, as 32-bit words, processed in place.</param>
    /// <param name="costN">The CPU/memory cost parameter <c>N</c>.</param>
    /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
    /// <param name="kernel">The BlockMix kernel; not <see cref="KernelKind.Auto" />.</param>
    /// <param name="pool">The pool the workspace is taken from and returned to.</param>
    private static void Mix(Span<uint> words, int costN, int blockSizeR, KernelKind kernel, NativeBufferPool pool)
    {
        int unitWords = 2 * blockSizeR * BlockWords;

        using Workspace workspace = Workspace.Rent(costN, unitWords, pool);
        for (int offset = 0; offset < words.Length; offset += unitWords)
            ROMix(words.Slice(offset, unitWords), costN, blockSizeR, workspace.Chain, workspace.Scratch, kernel);
    }

    /// <summary>
    /// Applies ROMix to every unit of <c>B</c>, dividing the units among threads, each with a workspace of its own.
    /// </summary>
    /// <param name="b">The array holding <c>B</c>.</param>
    /// <param name="length">The length of <c>B</c>, in bytes.</param>
    /// <param name="costN">The CPU/memory cost parameter <c>N</c>.</param>
    /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
    /// <param name="parallelization">The number of units, <c>p</c>.</param>
    /// <param name="workers">The number of threads, the calling thread included.</param>
    /// <param name="kernel">The BlockMix kernel; not <see cref="KernelKind.Auto" />.</param>
    /// <param name="pool">The pool the workspaces are taken from and returned to.</param>
    /// <remarks>
    /// <para>
    /// Each thread claims units from a shared counter until none are left, and takes its workspace only once it has
    /// claimed one, so a thread that starts after the work is done allocates nothing. The calling thread claims units
    /// too, and runs them all if no worker arrives, so a starved thread pool slows a derivation down but never stalls
    /// it. The default scheduler is named explicitly so a scheduler the caller runs under is never used.
    /// </para>
    /// <para>
    /// A fault in a unit surfaces as itself rather than wrapped in an <see cref="AggregateException" />, and only after
    /// every worker has stopped, so <c>B</c> is never cleared or returned while a worker could still write to it.
    /// </para>
    /// </remarks>
    private static void MixInParallel(byte[] b, int length, int costN, int blockSizeR, int parallelization, int workers, KernelKind kernel, NativeBufferPool pool)
    {
        int unitWords = 2 * blockSizeR * BlockWords;
        int nextUnit = -1;
        var options = new ParallelOptions { MaxDegreeOfParallelism = workers, TaskScheduler = TaskScheduler.Default };

        try
        {
            Parallel.For(0, workers, options, _ =>
            {
                int unit = Interlocked.Increment(ref nextUnit);
                if (unit >= parallelization)
                    return;

                Span<uint> words = MemoryMarshal.Cast<byte, uint>(b.AsSpan(0, length));
                using Workspace workspace = Workspace.Rent(costN, unitWords, pool);
                do
                {
                    ROMix(words.Slice(unit * unitWords, unitWords), costN, blockSizeR, workspace.Chain, workspace.Scratch, kernel);
                    unit = Interlocked.Increment(ref nextUnit);
                }
                while (unit < parallelization);
            });
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count > 0)
        {
            ExceptionDispatchInfo.Capture(ex.InnerExceptions[0]).Throw();
        }
    }

    /// <summary>
    /// Selects the widest BlockMix kernel the processor supports and the process allows: AdvSimd on ARM64, then SSE2 on
    /// x64, then the scalar kernel.
    /// </summary>
    /// <returns>The kernel dispatch runs; never <see cref="KernelKind.Auto" />.</returns>
    /// <remarks>
    /// Every gate honors the <see cref="SimdCapabilities.DisableSimdSwitchName" /> switch, which pins the scalar
    /// kernel.
    /// </remarks>
    internal static KernelKind SelectKernel()
    {
        if (SimdCapabilities.AdvSimd)
            return KernelKind.AdvSimd;

        return SimdCapabilities.Sse2 ? KernelKind.Sse2 : KernelKind.Scalar;
    }

    /// <summary>
    /// Determines whether the processor can run the specified BlockMix kernel, whether or not the process allows vector
    /// code.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <returns>
    /// <see langword="true" /> if the processor supports every instruction the kernel uses; otherwise,
    /// <see langword="false" />.
    /// </returns>
    internal static bool IsSupported(KernelKind kernel) => kernel switch
    {
        KernelKind.Auto or KernelKind.Scalar => true,
        KernelKind.Sse2 => System.Runtime.Intrinsics.X86.Sse2.IsSupported,
        KernelKind.AdvSimd => System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported,
        _ => false,
    };

    /// <summary>
    /// Applies <c>scryptROMix</c> to a single 128·r-byte block in place (RFC 7914, Section 5), with the kernel dispatch
    /// selects.
    /// </summary>
    /// <param name="block">The 128·r-byte block <c>X</c>, as 32-bit words, processed in place.</param>
    /// <param name="costN">The CPU/memory cost parameter <c>N</c>.</param>
    /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
    /// <param name="v">The <c>N</c> units of <c>V</c>; their contents on entry are ignored.</param>
    /// <param name="scratch">One unit of working space; its contents on entry are ignored.</param>
    internal static void ROMix(Span<uint> block, int costN, int blockSizeR, Span<uint> v, Span<uint> scratch) =>
        ROMix(block, costN, blockSizeR, v, scratch, SelectKernel());

    /// <summary>
    /// Applies <c>scryptROMix</c> to a single 128·r-byte block in place (RFC 7914, Section 5), with the specified
    /// kernel.
    /// </summary>
    /// <param name="block">The 128·r-byte block <c>X</c>, as 32-bit words, processed in place.</param>
    /// <param name="costN">The CPU/memory cost parameter <c>N</c>.</param>
    /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
    /// <param name="v">The <c>N</c> units of <c>V</c>; their contents on entry are ignored.</param>
    /// <param name="scratch">One unit of working space; its contents on entry are ignored.</param>
    /// <param name="kernel">
    /// The BlockMix kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <remarks>
    /// The kernel is chosen once per unit; each branch runs a loop the JIT specializes for its kernel, so no block pays
    /// for the choice.
    /// </remarks>
    internal static void ROMix(Span<uint> block, int costN, int blockSizeR, Span<uint> v, Span<uint> scratch, KernelKind kernel)
    {
        switch (kernel == KernelKind.Auto ? SelectKernel() : kernel)
        {
            case KernelKind.AdvSimd:
                ROMix<Vector128Kernel<AdvSimdIsa>>(block, costN, blockSizeR, v, scratch);
                break;

            case KernelKind.Sse2:
                ROMix<Vector128Kernel<Sse2Isa>>(block, costN, blockSizeR, v, scratch);
                break;

            default:
                ROMix<ScalarKernel>(block, costN, blockSizeR, v, scratch);
                break;
        }
    }

    /// <summary>
    /// Applies <c>scryptROMix</c> to a single 128·r-byte block in place (RFC 7914, Section 5).
    /// </summary>
    /// <typeparam name="TKernel">The BlockMix kernel.</typeparam>
    /// <param name="block">The 128·r-byte block <c>X</c>, as 32-bit words, processed in place.</param>
    /// <param name="costN">The CPU/memory cost parameter <c>N</c>.</param>
    /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
    /// <param name="v">The <c>N</c> units of <c>V</c>; their contents on entry are ignored.</param>
    /// <param name="scratch">One unit of working space; its contents on entry are ignored.</param>
    /// <remarks>
    /// Every unit of <paramref name="v" /> is written before it can be read, so its contents on entry cannot affect the
    /// result. Clearing <paramref name="v" /> and <paramref name="scratch" /> afterwards is the caller's
    /// responsibility.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void ROMix<TKernel>(Span<uint> block, int costN, int blockSizeR, Span<uint> v, Span<uint> scratch)
        where TKernel : struct, IScryptKernel
    {
        int unitWords = block.Length;
        ref uint x = ref MemoryMarshal.GetReference(block);
        ref uint y = ref MemoryMarshal.GetReference(scratch);
        ref uint v0 = ref MemoryMarshal.GetReference(v);

        // The unit stays in the kernel's word order from here until the end, V included.
        TKernel.Import(ref x, 2 * blockSizeR);

        // V[0] = X and V[i + 1] = BlockMix(V[i]): each link is written in place, and the last one becomes X.
        block.CopyTo(v);
        ref uint link = ref v0;
        for (int i = 1; i < costN; i++)
        {
            ref uint next = ref Unsafe.Add(ref link, unitWords);
            TKernel.BlockMix(ref link, ref next, blockSizeR);
            link = ref next;
        }

        TKernel.BlockMix(ref link, ref x, blockSizeR);

        // X = BlockMix(X xor V[j]), with j = Integerify(X) mod N. N is a power of two no greater than 2^30, so the
        // index is the low word of X's last 64-byte block masked to N - word 0, which every kernel keeps first. The
        // result alternates between the block and the scratch, and N being even leaves it in the block.
        int integerify = unitWords - BlockWords;
        uint mask = (uint)costN - 1;
        for (int i = 0; i < costN; i += 2)
        {
            nint j = (nint)(Unsafe.Add(ref x, integerify) & mask);
            TKernel.BlockMixXor(ref x, ref Unsafe.Add(ref v0, j * unitWords), ref y, blockSizeR);

            j = (nint)(Unsafe.Add(ref y, integerify) & mask);
            TKernel.BlockMixXor(ref y, ref Unsafe.Add(ref v0, j * unitWords), ref x, blockSizeR);
        }

        TKernel.Export(ref x, 2 * blockSizeR);
    }
}
