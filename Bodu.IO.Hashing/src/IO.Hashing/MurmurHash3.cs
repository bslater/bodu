// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Bodu.IO.Hashing;

/// <summary>
/// Base class for the <c>MurmurHash3</c> family of non-cryptographic hash algorithms by Austin Appleby. See the
/// <a href="https://github.com/aappleby/smhasher">SMHasher reference repository</a> for the specification.
/// </summary>
/// <remarks>
/// <para>
/// The algorithm is block-streamable: input delivered through <see cref="Append(ReadOnlySpan{byte})" /> is mixed into
/// the running accumulators one block at a time, with only a partial trailing block buffered between calls. Memory use
/// is constant regardless of input length, and reading the current hash applies the tail and finalization mix to a copy
/// of the accumulators, so digest reads are non-destructive.
/// </para>
/// <para>
/// A 32-bit seed can be supplied at construction time to vary the output for the same input, which is useful for
/// building distributed hash tables and bloom filters. The seed does not affect the algorithm's security posture.
/// </para>
/// <para>
/// Shared mixing primitives (<see cref="FMix32(uint)" />, <see cref="FMix64(ulong)" />) and algorithm constants are
/// defined here and are available to all derived variants. Supported output sizes are 32 and 128 bits.
/// </para>
/// <para>
/// <strong>When to choose MurmurHash3.</strong> MurmurHash3 has excellent avalanche, strong distribution under
/// SMHasher's full battery, and is faster than the FNV family on inputs longer than a few dozen bytes — making it the
/// default choice for non-distributed in-memory hash tables, bloom filters, and content-based sharding. Pick
/// <see cref="MurmurHash3_32" /> when 32 bits is sufficient and the host is 32-bit-friendly; pick
/// <see cref="MurmurHash3_128" /> when collision pressure (large key spaces, fingerprinting) calls for more bits.
/// <see cref="CityHash" /> typically edges MurmurHash3 on long inputs on 64-bit CPUs; <see cref="Fnv" /> is preferable
/// only for very small fixed-length keys.
/// </para>
/// <para>
/// Instances are not thread-safe; share behind explicit synchronization.
/// </para>
/// <note type="important"> MurmurHash3 is <b>not</b> cryptographically secure. It must <b>not</b> be used for password
/// hashing, digital signatures, or any application that requires collision resistance under adversarial conditions.
/// </note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Extensions;
///
/// // 32-bit hash, default seed, suitable for in-memory hash tables.
/// var m32 = new MurmurHash3_32();
/// uint h32 = BinaryPrimitives.ReadUInt32LittleEndian(m32.ComputeHash(keyBytes));
///
/// // 128-bit hash, custom seed for shard isolation across services.
/// var m128 = new MurmurHash3_128(seed: 0xC2B2AE35u);
/// byte[] fingerprint = m128.ComputeHash(payload);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="MurmurHash3_32"/> <seealso cref="MurmurHash3_128"/>
public abstract class MurmurHash3
    : NonCryptographicHashAlgorithm, IDisposable
{
    /// <summary>The set of hash output sizes, in bits, that this algorithm family supports.</summary>
    private static readonly int[] s_validHashSizes = [32, 128];

    /// <summary>Buffers a partial trailing block between <see cref="Append(ReadOnlySpan{byte})" /> calls. Sized to the variant's block size.</summary>
    private readonly byte[] _tail;

    /// <summary>The number of bytes currently held in <see cref="_tail" />, always less than the block size after each call completes.</summary>
    private int _tailLength;

    /// <summary>The running total of bytes appended since construction or the last reset, folded into the finalization mix.</summary>
    private ulong _totalBytes;

    /// <summary>Indicates whether this instance has been disposed and its state cleared.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MurmurHash3" /> class with the specified hash output size, block
    /// size, and seed.
    /// </summary>
    /// <param name="hashSize">The desired hash output size in bits. Must be one of 32 or 128.</param>
    /// <param name="blockSizeBytes">
    /// The variant's block size, in bytes (4 for the 32-bit variant, 16 for the 128-bit variant).
    /// </param>
    /// <param name="seed">The 32-bit seed value used to initialize the hash state.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not one of the supported values (32 or 128).
    /// </exception>
    private protected MurmurHash3(int hashSize, int blockSizeBytes, uint seed = 0)
        : base(ValidateHashSize(hashSize) / 8)
    {
        Seed = seed;
        _tail = new byte[blockSizeBytes];
    }

    /// <summary>
    /// Gets the 32-bit seed used to initialize the hash computation.
    /// </summary>
    /// <value>The seed value supplied at construction time, or zero if no seed was specified.</value>
    public uint Seed { get; }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _totalBytes += (ulong)source.Length;
        int blockBytes = _tail.Length;

        // Top up a pending partial block first; mix it as soon as it completes.
        if (_tailLength > 0)
        {
            int take = Math.Min(blockBytes - _tailLength, source.Length);
            source[..take].CopyTo(_tail.AsSpan(_tailLength));
            _tailLength += take;
            source = source[take..];

            if (_tailLength == blockBytes)
            {
                MixBlocks(_tail);
                _tailLength = 0;
            }
        }

        // Mix the aligned run directly from the caller's buffer.
        int aligned = source.Length - (source.Length % blockBytes);
        if (aligned > 0)
        {
            MixBlocks(source[..aligned]);
            source = source[aligned..];
        }

        // Buffer the remaining partial block for the next call or finalization.
        source.CopyTo(_tail.AsSpan(_tailLength));
        _tailLength += source.Length;
    }

    /// <summary>
    /// Releases all resources used by the current instance and clears its buffered input state.
    /// </summary>
    /// <remarks>
    /// After disposal, subsequent calls to <see cref="Append(ReadOnlySpan{byte})" />, <see cref="Reset" />, or
    /// <see cref="GetCurrentHashCore(Span{byte})" /> throw <see cref="ObjectDisposedException" />. Calling
    /// <see cref="Dispose()" /> multiple times is safe and has no effect after the first invocation.
    /// </remarks>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public override void Reset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _tailLength = 0;
        _totalBytes = 0;
        ResetCore();
    }

    /// <summary>
    /// Applies the MurmurHash3 32-bit finalization mix to thoroughly diffuse the bits of a 32-bit value.
    /// </summary>
    /// <param name="h">The 32-bit value to mix.</param>
    /// <returns>The finalized 32-bit value with strong avalanche properties.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint FMix32(uint h)
    {
        h ^= h >> 16;
        h = unchecked(h * 0x85EBCA6Bu);
        h ^= h >> 13;
        h = unchecked(h * 0xC2B2AE35u);
        h ^= h >> 16;
        return h;
    }

    /// <summary>
    /// Applies the MurmurHash3 64-bit finalization mix to thoroughly diffuse the bits of a 64-bit value.
    /// </summary>
    /// <param name="k">The 64-bit value to mix.</param>
    /// <returns>The finalized 64-bit value with strong avalanche properties.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ulong FMix64(ulong k)
    {
        k ^= k >> 33;
        k = unchecked(k * 0xFF51AFD7ED558CCDuL);
        k ^= k >> 33;
        k = unchecked(k * 0xC4CEB9FE1A85EC53uL);
        k ^= k >> 33;
        return k;
    }

    /// <summary>
    /// Resets the variant's running accumulators to the seeded initial state.
    /// </summary>
    private protected abstract void ResetCore();

    /// <summary>
    /// Mixes a run of one or more complete blocks into the variant's running accumulators.
    /// </summary>
    /// <param name="blocks">
    /// The block-aligned input; its length is always a positive multiple of the block size.
    /// </param>
    private protected abstract void MixBlocks(ReadOnlySpan<byte> blocks);

    /// <summary>
    /// Applies the tail bytes and the finalization mix to a copy of the running accumulators and writes the digest,
    /// leaving the running state untouched so digest reads remain non-destructive.
    /// </summary>
    /// <param name="tail">The buffered partial trailing block (possibly empty).</param>
    /// <param name="totalBytes">The total number of bytes appended so far.</param>
    /// <param name="destination">The span receiving the digest; exactly the hash length in bytes.</param>
    private protected abstract void FinalizeCore(ReadOnlySpan<byte> tail, ulong totalBytes, Span<byte> destination);

    /// <summary>
    /// Releases the resources used by the current instance, optionally clearing managed state.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> when called from <see cref="Dispose()" />; <see langword="false" /> when called from a
    /// finalizer. Managed resources are released only when <paramref name="disposing" /> is <see langword="true" />.
    /// </param>
    /// <remarks>
    /// Override in a derived class to release additional resources owned by the subclass. Always invoke
    /// <c>base.Dispose(disposing)</c> from the override so that the buffered state is cleared.
    /// </remarks>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Array.Clear(_tail);
            _tailLength = 0;
            _totalBytes = 0;
        }

        _disposed = true;
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        FinalizeCore(_tail.AsSpan(0, _tailLength), _totalBytes, destination[..HashLengthInBytes]);
    }

    /// <summary>
    /// Validates that <paramref name="hashSize" /> is one of the hash sizes this algorithm supports.
    /// </summary>
    /// <param name="hashSize">The requested hash size, in bits.</param>
    /// <returns>The validated <paramref name="hashSize" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not one of the supported hash sizes.
    /// </exception>
    private static int ValidateHashSize(int hashSize)
    {
        HashingThrowHelper.ThrowIfInvalidHashSize(hashSize, s_validHashSizes);
        return hashSize;
    }
}
