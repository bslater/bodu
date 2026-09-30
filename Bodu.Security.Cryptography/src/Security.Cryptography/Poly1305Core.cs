// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Core.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the Poly1305 one-time authenticator (RFC 8439, Section 2.5) over three 64-bit limbs of 44, 44 and 42
/// bits, for <see cref="Poly1305" /> and the Poly1305 AEADs.
/// </summary>
/// <remarks>
/// <para>
/// The accumulator and the clamped key half <c>r</c> are each three limbs, so a block's multiplication is nine 64 × 64
/// → 128-bit products. The bits a product carries past 2^130 fold back multiplied by 5, which the precomputed
/// <c>20 · r1</c> and <c>20 · r2</c> already carry: a limb product that lands at 2^132 is 4 · 2^130. Each product is
/// split at bit 44 as it is formed, so the limb sums stay within 64 bits and need no 128-bit carries. Every step is
/// free of branches, and the final reduction chooses between <c>h</c> and <c>h − p</c> with masks, so the time taken
/// depends only on the message length.
/// </para>
/// <para>
/// Long runs of whole blocks go through vector kernels instead, which give each 64-bit lane a block of its own: four
/// lanes with AVX2 and eight with AVX-512 on x64, two with AdvSimd on ARM64. In a vector the accumulator and <c>r</c>
/// are five limbs of 26 bits, because the only vector multiply is 32 × 32 → 64 bits. Each lane multiplies by <c>r²</c>,
/// <c>r⁴</c> or <c>r⁸</c> per group of blocks, and by the power of <c>r</c> its last block needs at the end, so the
/// lanes sum to exactly the accumulator the scalar loop reaches. Each run computes its powers of <c>r</c> afresh, with
/// one to eight scalar multiplications, so <see cref="SelectKernel" /> keeps runs too short to repay them on the scalar
/// loop, at thresholds measured on both runtimes.
/// </para>
/// <para>
/// The core is a mutable struct that callers keep in a field or a local and use in place. It holds a partial block
/// between calls to <see cref="Update(ReadOnlySpan{byte})" />, and <see cref="Finish" /> clears the whole struct, key
/// included, once the tag is written.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal partial struct Poly1305Core
{
    /// <summary>The number of bytes in a Poly1305 one-time key: <c>r</c> followed by <c>s</c>.</summary>
    internal const int KeyBytes = 32;

    /// <summary>The number of bytes in a Poly1305 tag.</summary>
    internal const int TagBytes = 16;

    /// <summary>The number of message bytes in a Poly1305 block.</summary>
    internal const int BlockBytes = 16;

    /// <summary>The mask of a 44-bit limb.</summary>
    private const ulong Mask44 = (1UL << 44) - 1;

    /// <summary>The mask of the 42-bit top limb.</summary>
    private const ulong Mask42 = (1UL << 42) - 1;

    /// <summary>The bit a full block sets above its 128 message bits, 2^128, as it falls in the top limb, which starts at 2^88.</summary>
    private const ulong FullBlockBit = 1UL << 40;

    /// <summary>The mask of a 26-bit limb, the width the vector kernels work in.</summary>
    private const ulong Mask26 = (1UL << 26) - 1;

    /// <summary>The bit a full block sets above its 128 message bits, 2^128, as it falls in the top 26-bit limb, which starts at 2^104.</summary>
    private const ulong FullBlockBit26 = 1UL << 24;

    /// <summary>The shortest run of whole blocks, in bytes, that dispatch gives the four-lane AVX2 kernel.</summary>
    /// <remarks>
    /// Measured on .NET 8 and .NET 10, the kernel beats the scalar loop from 192 to 256 bytes when the MAC runs alone.
    /// Inside an AEAD it pays only from about 512 bytes: below that, the Poly1305 AEADs' messages ran up to 10% slower
    /// through it, as its 256-bit multiplies cost the rest of the message more than they save.
    /// </remarks>
    internal const int Avx2MinimumBytes = 512;

    /// <summary>The shortest run of whole blocks, in bytes, that dispatch gives the AVX2 kernel's paired loop, where AVX-512VL provides its registers.</summary>
    /// <remarks>
    /// Below it, computing <c>r⁸</c> costs more than pairing the groups saves: measured on .NET 8 and .NET 10, the
    /// paired loop first beats the one-group loop at 512 bytes to 1 KiB.
    /// </remarks>
    internal const int Avx2PairedMinimumBytes = 1024;

    /// <summary>The shortest run of whole blocks, in bytes, that dispatch gives the eight-lane AVX-512 kernel.</summary>
    /// <remarks>
    /// Below it, the four lanes of the AVX2 kernel finish first: measured on .NET 8 and .NET 10, the crossover lies
    /// between 2 and 4 KiB.
    /// </remarks>
    internal const int Avx512MinimumBytes = 4096;

    /// <summary>The shortest run of whole blocks, in bytes, that dispatch gives the two-lane AdvSimd kernel on ARM64.</summary>
    /// <remarks>
    /// Measured on .NET 8 and .NET 10, the kernel caught the scalar loop at 256 bytes on a Neoverse N2, and at 128 on
    /// an Apple M1; below 256 bytes the N2's scalar loop, with <c>umulh</c>, finished first.
    /// </remarks>
    internal const int AdvSimdMinimumBytes = 256;

    /// <summary>
    /// Gets the shortest run of whole blocks, in bytes, that any kernel dispatch may select on this processor takes:
    /// <see cref="AdvSimdMinimumBytes" /> on ARM64 and <see cref="Avx2MinimumBytes" /> elsewhere. Shorter runs go
    /// straight to the scalar loop.
    /// </summary>
    internal static int KernelMinimumBytes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => AdvSimd.Arm64.IsSupported ? AdvSimdMinimumBytes : Avx2MinimumBytes;
    }

    /// <summary>The first limb of the clamped key half <c>r</c>.</summary>
    private ulong _r0;

    /// <summary>The second limb of <c>r</c>.</summary>
    private ulong _r1;

    /// <summary>The third limb of <c>r</c>.</summary>
    private ulong _r2;

    /// <summary><c>20 · r1</c>: the second limb of <c>r</c> as it multiplies into the lowest limb after the fold.</summary>
    private ulong _s1;

    /// <summary><c>20 · r2</c>: the third limb of <c>r</c> as it multiplies into the lower two limbs after the fold.</summary>
    private ulong _s2;

    /// <summary>The first limb of the accumulator <c>h</c>.</summary>
    private ulong _h0;

    /// <summary>The second limb of the accumulator.</summary>
    private ulong _h1;

    /// <summary>The third limb of the accumulator.</summary>
    private ulong _h2;

    /// <summary>The low 64 bits of the key half <c>s</c>, added to the accumulator to form the tag.</summary>
    private ulong _pad0;

    /// <summary>The high 64 bits of <c>s</c>.</summary>
    private ulong _pad1;

    /// <summary>The first eight message bytes of a block not yet complete; with <see cref="_pending1" />, which the sequential layout places straight after it, the 16 bytes of <see cref="Pending" />.</summary>
    private ulong _pending0;

    /// <summary>The last eight message bytes of a block not yet complete.</summary>
    private ulong _pending1;

    /// <summary>The number of bytes held in <see cref="Pending" />.</summary>
    private int _pendingLength;

    /// <summary>
    /// Gets the 16 bytes that hold a block not yet complete, over <see cref="_pending0" /> and <see cref="_pending1" />.
    /// </summary>
    private Span<byte> Pending =>
        MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref _pending0, 2));

    /// <summary>
    /// Starts a new authentication under a one-time key, discarding any state the core held.
    /// </summary>
    /// <param name="key">The 32-byte one-time key: <c>r</c>, which is clamped, then <c>s</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="key" /> is not 32 bytes long.</exception>
    internal void Initialize(ReadOnlySpan<byte> key)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(key.Length, KeyBytes, nameof(key));

        // r &= 0x0ffffffc0ffffffc0ffffffc0fffffff (RFC 8439, Section 2.5), split into limbs of 44, 44 and 42 bits.
        ulong t0 = BinaryPrimitives.ReadUInt64LittleEndian(key);
        ulong t1 = BinaryPrimitives.ReadUInt64LittleEndian(key[8..]);
        _r0 = t0 & 0xFFC0FFFFFFFUL;
        _r1 = ((t0 >> 44) | (t1 << 20)) & 0xFFFFFC0FFFFUL;
        _r2 = (t1 >> 24) & 0x00FFFFFFC0FUL;
        _s1 = _r1 * (5 << 2);
        _s2 = _r2 * (5 << 2);

        _h0 = 0;
        _h1 = 0;
        _h2 = 0;
        _pad0 = BinaryPrimitives.ReadUInt64LittleEndian(key[16..]);
        _pad1 = BinaryPrimitives.ReadUInt64LittleEndian(key[24..]);

        _pending0 = 0;
        _pending1 = 0;
        _pendingLength = 0;
    }

    /// <summary>
    /// Absorbs message bytes, holding back a partial block until more arrive or <see cref="Finish" /> is called.
    /// </summary>
    /// <param name="data">The message bytes.</param>
    internal void Update(ReadOnlySpan<byte> data) =>
        Update(KernelKind.Auto, data);

    /// <summary>
    /// Absorbs message bytes through the specified kernel, holding back a partial block until more arrive or
    /// <see cref="Finish" /> is called.
    /// </summary>
    /// <param name="kernel">
    /// The kernel for the run of whole blocks, or <see cref="KernelKind.Auto" /> for the one dispatch selects.
    /// </param>
    /// <param name="data">The message bytes.</param>
    /// <exception cref="PlatformNotSupportedException">
    /// <paramref name="kernel" /> names a kernel the processor does not support, and the run of whole blocks is long
    /// enough to reach it.
    /// </exception>
    /// <remarks>
    /// Every kernel produces the accumulator of the scalar loop. A kernel named explicitly runs whenever the whole
    /// blocks fill at least one group of its lanes, whatever the thresholds <see cref="SelectKernel" /> applies, so
    /// that tests can drive it over short messages; the blocks after its last whole group, and everything shorter, go
    /// through the scalar loop.
    /// </remarks>
    internal void Update(KernelKind kernel, ReadOnlySpan<byte> data)
    {
        if (_pendingLength > 0)
        {
            Span<byte> pending = Pending;
            int take = Math.Min(BlockBytes - _pendingLength, data.Length);
            data[..take].CopyTo(pending[_pendingLength..]);
            _pendingLength += take;
            data = data[take..];

            if (_pendingLength < BlockBytes)
                return;

            Blocks(pending, FullBlockBit);
            _pendingLength = 0;
        }

        int whole = data.Length & ~(BlockBytes - 1);
        if (whole > 0)
        {
            FullBlocks(kernel, data[..whole]);
            data = data[whole..];
        }

        if (!data.IsEmpty)
        {
            data.CopyTo(Pending);
            _pendingLength = data.Length;
        }
    }

    /// <summary>
    /// Absorbs message bytes followed by zeros up to the next 16-byte boundary, as RFC 8439's AEAD construction pads
    /// its associated data and its ciphertext (Section 2.8).
    /// </summary>
    /// <param name="data">The message bytes.</param>
    /// <remarks>
    /// The zero padding makes each padded segment a whole number of full blocks, each with the 2^128 bit set, which is
    /// not what <see cref="Finish" /> does with a partial last block.
    /// </remarks>
    internal void UpdatePadded(ReadOnlySpan<byte> data)
    {
        Update(data);

        if (_pendingLength > 0)
        {
            Span<byte> pending = Pending;
            pending[_pendingLength..].Clear();
            Blocks(pending, FullBlockBit);
            _pendingLength = 0;
        }
    }

    /// <summary>
    /// Completes the authentication: absorbs a partial last block, reduces the accumulator, adds <c>s</c>, writes the
    /// tag, and clears the core.
    /// </summary>
    /// <param name="tag">The 16 bytes that receive the tag.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tag" /> holds fewer than 16 bytes.</exception>
    /// <remarks>
    /// A partial last block is padded with a single 1 byte and zeros, and absorbed without the 2^128 bit (RFC 8439,
    /// Section 2.5.1).
    /// </remarks>
    internal void Finish(Span<byte> tag)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tag.Length, TagBytes, nameof(tag));

        if (_pendingLength > 0)
        {
            Span<byte> pending = Pending;
            pending[_pendingLength] = 1;
            pending.Slice(_pendingLength + 1).Clear();
            Blocks(pending, 0);
        }

        // Carry the accumulator fully, twice around the fold.
        ulong h0 = _h0;
        ulong h1 = _h1;
        ulong h2 = _h2;
        ulong c = h1 >> 44;
        h1 &= Mask44;
        h2 += c;
        c = h2 >> 42;
        h2 &= Mask42;
        h0 += c * 5;
        c = h0 >> 44;
        h0 &= Mask44;
        h1 += c;
        c = h1 >> 44;
        h1 &= Mask44;
        h2 += c;
        c = h2 >> 42;
        h2 &= Mask42;
        h0 += c * 5;
        c = h0 >> 44;
        h0 &= Mask44;
        h1 += c;

        // g = h + 5 − 2^130 = h − p; its top limb borrows exactly when h < p.
        ulong g0 = h0 + 5;
        c = g0 >> 44;
        g0 &= Mask44;
        ulong g1 = h1 + c;
        c = g1 >> 44;
        g1 &= Mask44;
        ulong g2 = h2 + c - (1UL << 42);

        // All ones when g did not borrow (h ≥ p, so take g), all zeros when it did (take h).
        ulong useG = (g2 >> 63) - 1;
        h0 = (h0 & ~useG) | (g0 & useG);
        h1 = (h1 & ~useG) | (g1 & useG);
        h2 = (h2 & ~useG) | (g2 & useG);

        // tag = (h + s) mod 2^128.
        h0 += _pad0 & Mask44;
        c = h0 >> 44;
        h0 &= Mask44;
        h1 += (((_pad0 >> 44) | (_pad1 << 20)) & Mask44) + c;
        c = h1 >> 44;
        h1 &= Mask44;
        h2 += ((_pad1 >> 24) & Mask42) + c;
        h2 &= Mask42;

        BinaryPrimitives.WriteUInt64LittleEndian(tag, h0 | (h1 << 44));
        BinaryPrimitives.WriteUInt64LittleEndian(tag[8..], (h1 >> 20) | (h2 << 24));

        Clear();
    }

    /// <summary>
    /// Clears the whole core - key, accumulator and any held bytes - through a write the compiler cannot elide.
    /// </summary>
    internal void Clear() =>
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1)));

    /// <summary>
    /// Selects the kernel for a run of whole blocks. On x64: AVX-512 for long runs, AVX2 for shorter ones - two groups
    /// at a time from <see cref="Avx2PairedMinimumBytes" /> where AVX-512VL's registers hold them - and the scalar loop
    /// below <see cref="Avx2MinimumBytes" /> or where neither is available. On ARM64: AdvSimd from
    /// <see cref="AdvSimdMinimumBytes" />, and the scalar loop below.
    /// </summary>
    /// <param name="length">The length of the run, in bytes.</param>
    /// <returns>The kernel dispatch runs; never <see cref="KernelKind.Auto" />.</returns>
    /// <remarks>
    /// <para>
    /// Every gate honors the <see cref="SimdCapabilities.DisableSimdSwitchName" /> switch, which pins the scalar loop.
    /// </para>
    /// <para>
    /// AVX-512 runs only where <see cref="Vector512.IsHardwareAccelerated" /> holds. The runtime clears it on
    /// processors whose clock drops under sustained 512-bit work, so there the AVX2 kernel runs instead;
    /// <c>DOTNET_PreferredVectorBitWidth=512</c> opts such a processor in.
    /// </para>
    /// </remarks>
    internal static KernelKind SelectKernel(int length)
    {
        if (SimdCapabilities.AdvSimd)
            return length < AdvSimdMinimumBytes ? KernelKind.Scalar : KernelKind.AdvSimd;

        if (length >= Avx512MinimumBytes && SimdCapabilities.Avx512F && Vector512.IsHardwareAccelerated)
            return KernelKind.Avx512;

        if (length < Avx2MinimumBytes || !SimdCapabilities.Avx2)
            return KernelKind.Scalar;

        return length >= Avx2PairedMinimumBytes && SimdCapabilities.Avx512FVL ? KernelKind.Avx2Paired : KernelKind.Avx2;
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
        KernelKind.Avx2 or KernelKind.Avx2Paired => Avx2.IsSupported,
        KernelKind.Avx512 => Avx512F.IsSupported,
        KernelKind.AdvSimd => AdvSimd.Arm64.IsSupported,
        _ => false,
    };

    /// <summary>
    /// Returns the number of blocks a group of the specified kernel takes at once: one for each of its lanes.
    /// </summary>
    /// <param name="kernel">The kernel; not <see cref="KernelKind.Auto" />.</param>
    /// <returns>8 for AVX-512, 4 for either AVX2 loop, 2 for AdvSimd, and 1 for the scalar loop.</returns>
    internal static int LanesFor(KernelKind kernel) => kernel switch
    {
        KernelKind.Avx512 => 8,
        KernelKind.Avx2 or KernelKind.Avx2Paired => 4,
        KernelKind.AdvSimd => 2,
        _ => 1,
    };

    /// <summary>
    /// Absorbs a run of whole message blocks: the groups of blocks that fill the kernel's lanes through the kernel, and
    /// the blocks after them through the scalar loop.
    /// </summary>
    /// <param name="kernel">The kernel, or <see cref="KernelKind.Auto" /> for the one dispatch selects.</param>
    /// <param name="blocks">The blocks, a whole number of 16 bytes.</param>
    /// <exception cref="PlatformNotSupportedException">
    /// <paramref name="kernel" /> names a kernel the processor does not support, and <paramref name="blocks" /> fills
    /// at least one group of its lanes.
    /// </exception>
    /// <remarks>
    /// A run shorter than <see cref="KernelMinimumBytes" />, the shortest any kernel takes, goes straight to the scalar
    /// loop, and longer runs are dispatched out of line in <see cref="KernelBlocks" />, so a caller that inlines this
    /// method takes on only a comparison and two calls. The AEADs inline it into the methods that frame each message;
    /// with the dispatch inlined as well, those methods exceeded the JIT's inlining budget and left the small helpers
    /// of every message as calls.
    /// </remarks>
    private void FullBlocks(KernelKind kernel, ReadOnlySpan<byte> blocks)
    {
        if (kernel != KernelKind.Auto || blocks.Length >= KernelMinimumBytes)
            blocks = KernelBlocks(kernel, blocks);

        if (!blocks.IsEmpty)
            Blocks(blocks, FullBlockBit);
    }

    /// <summary>
    /// Absorbs the groups of blocks at the start of a run that fill the lanes of a vector kernel, and returns the
    /// blocks after them.
    /// </summary>
    /// <param name="kernel">The kernel, or <see cref="KernelKind.Auto" /> for the one dispatch selects.</param>
    /// <param name="blocks">The blocks, a whole number of 16 bytes.</param>
    /// <returns>
    /// The blocks after the kernel's last whole group; all of them when the kernel is the scalar loop, or when they
    /// fill no group.
    /// </returns>
    /// <exception cref="PlatformNotSupportedException">
    /// <paramref name="kernel" /> names a kernel the processor does not support, and <paramref name="blocks" /> fills
    /// at least one group of its lanes.
    /// </exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ReadOnlySpan<byte> KernelBlocks(KernelKind kernel, ReadOnlySpan<byte> blocks)
    {
        if (kernel == KernelKind.Auto)
            kernel = SelectKernel(blocks.Length);

        int lanes = LanesFor(kernel);
        int groups = blocks.Length / (lanes * BlockBytes);
        if (lanes == 1 || groups == 0)
            return blocks;

        ref byte message = ref MemoryMarshal.GetReference(blocks);
        if (kernel == KernelKind.Avx512)
            Vector512Kernel.Blocks(ref this, ref message, groups);
        else if (kernel == KernelKind.Avx2Paired)
            Vector256Kernel.BlocksPaired(ref this, ref message, groups);
        else if (kernel == KernelKind.Avx2)
            Vector256Kernel.Blocks(ref this, ref message, groups);
        else
            Vector128Kernel.Blocks(ref this, ref message, groups);

        int absorbed = groups * lanes * BlockBytes;
        return blocks[absorbed..];
    }

    /// <summary>
    /// Absorbs whole 16-byte blocks: for each, adds it to the accumulator and multiplies by <c>r</c> modulo 2^130 − 5.
    /// </summary>
    /// <param name="blocks">The blocks, a multiple of 16 bytes.</param>
    /// <param name="highBit">
    /// <see cref="FullBlockBit" /> for full message blocks; zero for a padded partial last block.
    /// </param>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private void Blocks(ReadOnlySpan<byte> blocks, ulong highBit)
    {
        ulong r0 = _r0;
        ulong r1 = _r1;
        ulong r2 = _r2;
        ulong s1 = _s1;
        ulong s2 = _s2;
        ulong h0 = _h0;
        ulong h1 = _h1;
        ulong h2 = _h2;

        ref byte message = ref MemoryMarshal.GetReference(blocks);
        for (nint offset = 0; offset + BlockBytes <= blocks.Length; offset += BlockBytes)
        {
            ulong t0 = ReadUInt64(ref message, offset);
            ulong t1 = ReadUInt64(ref message, offset + 8);
            h0 += t0 & Mask44;
            h1 += ((t0 >> 44) | (t1 << 20)) & Mask44;
            h2 += ((t1 >> 24) & Mask42) | highBit;

            Multiply(ref h0, ref h1, ref h2, r0, r1, r2, s1, s2);
        }

        _h0 = h0;
        _h1 = h1;
        _h2 = h2;
    }

    /// <summary>
    /// Multiplies a value by another modulo 2^130 − 5, both in limbs of 44, 44 and 42 bits, and reduces the product
    /// partially: every limb back under its width, apart from a small excess the second may carry.
    /// </summary>
    /// <param name="h0">The first limb of the value, replaced by the first limb of the product.</param>
    /// <param name="h1">The second limb of the value, replaced by the second limb of the product.</param>
    /// <param name="h2">The third limb of the value, replaced by the third limb of the product.</param>
    /// <param name="r0">The first limb of the multiplier.</param>
    /// <param name="r1">The second limb of the multiplier.</param>
    /// <param name="r2">The third limb of the multiplier.</param>
    /// <param name="s1"><c>20 · r1</c>.</param>
    /// <param name="s2"><c>20 · r2</c>.</param>
    /// <remarks>
    /// The value's limbs may be as large as the accumulator's once a block is added, below 2^46, and the multiplier's
    /// as large as a partially reduced product's, so a product can serve as either factor of the next multiplication:
    /// the vector kernels form the powers of <c>r</c> this way.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Multiply(ref ulong h0, ref ulong h1, ref ulong h2, ulong r0, ulong r1, ulong r2, ulong s1, ulong s2)
    {
        // The limb sums of h·r, each product split at bit 44 as it is formed: sum k is highK · 2^44 + lowK.
        ulong low0 = 0;
        ulong high0 = 0;
        ulong low1 = 0;
        ulong high1 = 0;
        ulong low2 = 0;
        ulong high2 = 0;
        AddProduct(h0, r0, ref low0, ref high0);
        AddProduct(h1, s2, ref low0, ref high0);
        AddProduct(h2, s1, ref low0, ref high0);
        AddProduct(h0, r1, ref low1, ref high1);
        AddProduct(h1, r0, ref low1, ref high1);
        AddProduct(h2, s2, ref low1, ref high1);
        AddProduct(h0, r2, ref low2, ref high2);
        AddProduct(h1, r1, ref low2, ref high2);
        AddProduct(h2, r0, ref low2, ref high2);

        // A partial reduction: each limb back under its width, what passes 2^130 folded into the first times 5.
        ulong c = high0 + (low0 >> 44);
        h0 = low0 & Mask44;
        low1 += c;
        c = high1 + (low1 >> 44);
        h1 = low1 & Mask44;
        low2 += c;
        c = (high2 << 2) + (low2 >> 42);
        h2 = low2 & Mask42;
        h0 += c * 5;
        c = h0 >> 44;
        h0 &= Mask44;
        h1 += c;
    }

    /// <summary>
    /// Adds a 64 × 64-bit product to a limb sum held as two parts: the product's low 44 bits to one, and the rest, the
    /// product shifted right 44 bits, to the other.
    /// </summary>
    /// <param name="left">The first factor.</param>
    /// <param name="right">The second factor.</param>
    /// <param name="low">The sum of the products' low 44 bits.</param>
    /// <param name="high">The sum of the products shifted right 44 bits.</param>
    /// <remarks>
    /// <para>
    /// The factors keep every product below 2^94, so a product shifted right 44 bits fits in 64 bits, and three of
    /// either part sum without overflow.
    /// </para>
    /// <para>
    /// The high half of the product comes from <c>mulx</c> on x64 with BMI2 and from <c>umulh</c> on ARM64. Without
    /// either, <see cref="Math.BigMul(ulong, ulong, out ulong)" /> is a call per product, so
    /// <see cref="SplitProduct" /> forms the split from three 64-bit multiplies inline instead.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddProduct(ulong left, ulong right, ref ulong low, ref ulong high)
    {
        if (Bmi2.X64.IsSupported || ArmBase.Arm64.IsSupported)
        {
            ulong product = left * right;
            low += product & Mask44;
            high += (MultiplyHigh(left, right) << 20) | (product >> 44);
        }
        else
        {
            high += SplitProduct(left, right, out ulong productLow);
            low += productLow;
        }
    }

    /// <summary>
    /// Splits the product of an accumulator limb and a limb of <c>r</c> or <c>20 · r</c> at bit 44, with three 64-bit
    /// multiplies: for processors without an instruction for the high half of a 64 × 64-bit product.
    /// </summary>
    /// <param name="left">The first factor, below 2^46: an accumulator limb.</param>
    /// <param name="right">The second factor, below 2^49: a limb of <c>r</c> or of <c>20 · r</c>.</param>
    /// <param name="low">Receives the product's low 44 bits.</param>
    /// <returns>The product shifted right 44 bits.</returns>
    /// <remarks>
    /// With <c>left = a1 · 2^32 + a0</c> and <c>right = b1 · 2^32 + b0</c>, the product is
    /// <c>(a1 · right + a0 · b1) · 2^32 + a0 · b0</c>. The bounds keep <c>a1 · right</c> below 2^63, so that sum, with
    /// the carry out of <c>a0 · b0</c>, is below 2^64: it is exactly the product's bits from 32 upward, from which both
    /// parts of the split follow.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong SplitProduct(ulong left, ulong right, out ulong low)
    {
        ulong leftLow = (uint)left;
        ulong lowProduct = leftLow * (uint)right;
        ulong upper = ((left >> 32) * right) + (leftLow * (right >> 32)) + (lowProduct >> 32);

        low = ((upper << 32) | (uint)lowProduct) & Mask44;
        return upper >> 12;
    }

    /// <summary>
    /// Returns the high 64 bits of the 128-bit product of two 64-bit values, through <c>mulx</c> on x64 with BMI2 or
    /// <c>umulh</c> on ARM64.
    /// </summary>
    /// <param name="left">The first factor.</param>
    /// <param name="right">The second factor.</param>
    /// <returns>The high half of the product.</returns>
    /// <remarks>
    /// Callers check that one of the two instructions is available; <see cref="SplitProduct" /> serves processors with
    /// neither.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MultiplyHigh(ulong left, ulong right) =>
        Bmi2.X64.IsSupported ? Bmi2.X64.MultiplyNoFlags(left, right) : ArmBase.Arm64.MultiplyHigh(left, right);

    /// <summary>
    /// Reads eight message bytes as a little-endian 64-bit value.
    /// </summary>
    /// <param name="message">The first byte of the message.</param>
    /// <param name="offset">The offset of the eight bytes.</param>
    /// <returns>The value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadUInt64(ref byte message, nint offset)
    {
        ulong value = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref message, offset));
        return BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);
    }
}
