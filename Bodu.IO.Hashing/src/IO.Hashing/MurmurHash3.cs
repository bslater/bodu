// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

using System.IO;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

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
/// streams. Instances are not thread-safe; share behind explicit synchronisation.
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
    : NonCryptographicHashAlgorithm
    where T : MurmurHash3<T>, new()
{
    private static readonly int[] ValidHashSizes = { 32, 128 };

    private readonly MemoryStream inputBuffer = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MurmurHash3{T}" /> class with the specified hash output
    /// size and seed.
    /// </summary>
    /// <param name="hashSize">The desired hash output size in bits. Must be one of 32 or 128.</param>
    /// <param name="seed">The 32-bit seed value used to initialise the hash state.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not one of the supported values (32 or 128).
    /// </exception>
    protected MurmurHash3(int hashSize, uint seed = 0)
        : base(ValidateHashSize(hashSize) / 8)
    {
        this.Seed = seed;
    }

    /// <summary>
    /// Gets the 32-bit seed used to initialise the hash computation.
    /// </summary>
    /// <returns>The seed value supplied at construction time, or zero if no seed was specified.</returns>
    public uint Seed { get; }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        if (source.Length > 0)
            this.inputBuffer.Write(source);
    }

    /// <inheritdoc />
    public override void Reset()
    {
        this.inputBuffer.SetLength(0);
        this.inputBuffer.Position = 0;
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        byte[] data = this.inputBuffer.ToArray();
        byte[] digest = this.ComputeHashCore(data);
        digest.AsSpan(0, this.HashLengthInBytes).CopyTo(destination);
    }

    /// <summary>
    /// Performs the full hash computation over the complete accumulated input in a single pass.
    /// </summary>
    /// <param name="source">The complete input bytes to hash.</param>
    /// <returns>A byte array containing the final hash output.</returns>
    protected abstract byte[] ComputeHashCore(ReadOnlySpan<byte> source);

    /// <summary>
    /// Applies the MurmurHash3 32-bit finalisation mix to thoroughly diffuse the bits of a 32-bit value.
    /// </summary>
    /// <param name="h">The 32-bit value to mix.</param>
    /// <returns>The finalised 32-bit value with strong avalanche properties.</returns>
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
    /// Applies the MurmurHash3 64-bit finalisation mix to thoroughly diffuse the bits of a 64-bit value.
    /// </summary>
    /// <param name="k">The 64-bit value to mix.</param>
    /// <returns>The finalised 64-bit value with strong avalanche properties.</returns>
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

    private static int ValidateHashSize(int hashSize)
    {
        if (Array.IndexOf(ValidHashSizes, hashSize) == -1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hashSize),
                hashSize,
                $"Invalid hash size: {hashSize}. Valid sizes are: {string.Join(", ", ValidHashSizes)}.");
        }

        return hashSize;
    }
}
