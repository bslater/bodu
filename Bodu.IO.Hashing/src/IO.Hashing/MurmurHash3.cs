// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Bodu.IO.Hashing;

/// <summary>
/// Base class for the <c>MurmurHash3</c> family of non-cryptographic hash algorithms by Austin Appleby. See the
/// <a href="https://github.com/aappleby/smhasher">SMHasher reference repository</a> for the specification.
/// </summary>
/// <typeparam name="T">
/// The concrete MurmurHash3 variant derived from this class. Must expose a public parameterless constructor.
/// </typeparam>
/// <remarks>
/// <para>
/// MurmurHash3 is a one-shot algorithm. To satisfy the incremental input contract of
/// <see cref="NonCryptographicHashAlgorithm" />, this base class accumulates all bytes delivered through
/// <see cref="Append(ReadOnlySpan{byte})" /> into an internal buffer and invokes the derived variant's
/// <see cref="ComputeHashCore(ReadOnlySpan{byte})" /> from <see cref="GetCurrentHashCore(Span{byte})" /> once
/// all input is available.
/// </para>
/// <para>
/// A 32-bit seed can be supplied at construction time to vary the output for the same input, which is useful for
/// building distributed hash tables and bloom filters. The seed does not affect the algorithm's security posture.
/// </para>
/// <para>
/// Shared mixing primitives (<see cref="FMix32(uint)" />, <see cref="FMix64(ulong)" />) and algorithm constants
/// are defined here and are available to all derived variants. Supported output sizes are 32 and 128 bits.
/// </para>
/// <para>
/// <strong>When to choose MurmurHash3.</strong> MurmurHash3 has excellent avalanche, strong distribution under
/// SMHasher's full battery, and is faster than the FNV family on inputs longer than a few dozen bytes — making
/// it the default choice for non-distributed in-memory hash tables, bloom filters, and content-based sharding.
/// Pick <see cref="MurmurHash3_32"/> when 32 bits is sufficient and the host is 32-bit-friendly; pick
/// <see cref="MurmurHash3_128"/> when collision pressure (large key spaces, fingerprinting) calls for more
/// bits. <see cref="CityHash{T}"/> typically edges MurmurHash3 on long inputs on 64-bit CPUs;
/// <see cref="Fnv{TSelf}"/> is preferable only for very small fixed-length keys.
/// </para>
/// <para>
/// <strong>Buffering caveat.</strong> Because the algorithm needs the whole message before mixing, the base
/// class buffers every appended byte until <see cref="GetCurrentHashCore(Span{byte})"/> is called. Memory
/// consumption therefore grows linearly with input length between resets — avoid feeding it multi-gigabyte
/// streams. Instances are not thread-safe; share behind explicit synchronization.
/// </para>
/// <note type="important">
/// MurmurHash3 is <b>not</b> cryptographically secure. It must <b>not</b> be used for password hashing, digital
/// signatures, or any application that requires collision resistance under adversarial conditions.
/// </note>
/// <example>
/// <code language="csharp">
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
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="MurmurHash3_32"/>
/// <seealso cref="MurmurHash3_128"/>
public abstract class MurmurHash3<T>
    : NonCryptographicHashAlgorithm, IDisposable
    where T : MurmurHash3<T>, new()
{

    private static readonly int[] s_validHashSizes = { 32, 128 };

    private readonly MemoryStream _inputBuffer = new MemoryStream();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MurmurHash3{T}" /> class with the specified hash output
    /// size and seed.
    /// </summary>
    /// <param name="hashSize">The desired hash output size in bits. Must be one of 32 or 128.</param>
    /// <param name="seed">The 32-bit seed value used to initialize the hash state.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not one of the supported values (32 or 128).
    /// </exception>
    protected MurmurHash3(int hashSize, uint seed = 0)
        : base(ValidateHashSize(hashSize) / 8)
    {
        this.Seed = seed;
    }

    /// <summary>
    /// Gets the 32-bit seed used to initialize the hash computation.
    /// </summary>
    /// <returns>The seed value supplied at construction time, or zero if no seed was specified.</returns>
    public uint Seed { get; }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);
        if (source.Length > 0)
            this._inputBuffer.Write(source);
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
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public override void Reset()
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);
        this._inputBuffer.SetLength(0);
        this._inputBuffer.Position = 0;
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
    /// Performs the full hash computation over the complete accumulated input in a single pass.
    /// </summary>
    /// <param name="source">The complete input bytes to hash.</param>
    /// <returns>A byte array containing the final hash output.</returns>
    protected abstract byte[] ComputeHashCore(ReadOnlySpan<byte> source);

    /// <summary>
    /// Releases the resources used by the current instance, optionally clearing managed state.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> when called from <see cref="Dispose()" />; <see langword="false" /> when called
    /// from a finalizer. Managed resources are released only when <paramref name="disposing" /> is
    /// <see langword="true" />.
    /// </param>
    /// <remarks>
    /// Override in a derived class to release additional resources owned by the subclass. Always invoke
    /// <c>base.Dispose(disposing)</c> from the override so that the buffered input state is released.
    /// </remarks>
    protected virtual void Dispose(bool disposing)
    {
        if (this._disposed)
            return;

        if (disposing)
        {
            this._inputBuffer.Dispose();
        }

        this._disposed = true;
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);
        byte[] data = this._inputBuffer.ToArray();
        byte[] digest = this.ComputeHashCore(data);
        digest.AsSpan(0, this.HashLengthInBytes).CopyTo(destination);
    }

    private static int ValidateHashSize(int hashSize)
    {
        HashingThrowHelper.ThrowIfInvalidHashSize(hashSize, s_validHashSizes);
        return hashSize;
    }

}
