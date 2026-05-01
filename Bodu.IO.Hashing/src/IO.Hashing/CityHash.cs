// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

using Bodu.Extensions;
using System.Buffers.Binary;
using System.IO;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

/// <summary>
/// Base class for the <c>CityHash</c> family of non-cryptographic hash algorithms developed by Google. See
/// the <a href="https://github.com/google/cityhash">CityHash reference repository</a> for the specification.
/// </summary>
/// <typeparam name="T">
/// The concrete CityHash variant derived from this class. Must expose a public parameterless constructor.
/// </typeparam>
/// <remarks>
/// <para>
/// CityHash is a one-shot algorithm. To satisfy the incremental input contract of
/// <see cref="NonCryptographicHashAlgorithm" />, this base class accumulates all bytes delivered through
/// <see cref="Append(ReadOnlySpan{byte})" /> into an internal buffer and invokes the derived variant's
/// <see cref="ComputeHashCore(ReadOnlySpan{byte})" /> from <see cref="GetCurrentHashCore(Span{byte})" /> once
/// all input is available.
/// </para>
/// <para>
/// Shared 32-bit mixing primitives (<see cref="Mix(uint)" />, <see cref="Mur(uint, uint)" />,
/// <see cref="Permute3(ref uint, ref uint, ref uint)" />) and shared 64-bit primitives
/// (<see cref="ShiftMix(ulong)" />, <see cref="HashLen16(ulong, ulong)" />,
/// <see cref="HashLen16(ulong, ulong, ulong)" />,
/// <see cref="WeakHashLen32WithSeeds(ulong, ulong, ulong, ulong, ulong, ulong)" />,
/// <see cref="WeakHashLen32WithSeeds(ReadOnlySpan{byte}, ulong, ulong)" />,
/// <see cref="Hash64Len0to16(ReadOnlySpan{byte})" />) and algorithm constants are defined here and are
/// available to all derived variants. Supported output sizes are 32, 64, and 128 bits.
/// </para>
/// <para>
/// <strong>When to choose CityHash.</strong> CityHash was designed for short-to-medium strings on 64-bit
/// CPUs and tends to outperform <see cref="MurmurHash3{T}"/> on long inputs while matching it on short ones.
/// It is the typical choice for in-memory hash tables, fingerprinting, and content-based sharding when both
/// throughput and distribution quality matter. Pick <see cref="CityHash32"/> for 32-bit slot indexes,
/// <see cref="CityHash64"/> for general-purpose 64-bit hashing, and <see cref="CityHash128"/> for low-collision
/// fingerprinting of large key spaces. For very small fixed-length keys, <see cref="Fnv{TSelf}"/> is simpler
/// and competitive; for adversarial inputs use a member of <c>Bodu.Security.Cryptography</c> instead.
/// </para>
/// <para>
/// <strong>Buffering caveat.</strong> Because the algorithm needs the whole message before mixing, the base
/// class buffers every appended byte until <see cref="GetCurrentHashCore(Span{byte})"/> is called. Memory
/// consumption grows linearly with input length between resets — avoid feeding it multi-gigabyte streams.
/// Instances are not thread-safe; share behind explicit synchronisation.
/// </para>
/// <note type="important">
/// CityHash is <b>not</b> cryptographically secure. It must <b>not</b> be used for password hashing, digital
/// signatures, or any application that requires collision resistance under adversarial conditions.
/// </note>
/// <example>
/// <code language="csharp">
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Extensions;
///
/// // 64-bit fingerprint of a content blob — typical use case.
/// var city = new CityHash64();
/// byte[] fingerprint = city.ComputeHash(blob);
///
/// // Stream-hash a moderately sized file. Note: CityHash buffers fully — prefer Crc / xxHash for very large streams.
/// using FileStream fs = File.OpenRead("payload.bin");
/// byte[] streamDigest = city.ComputeHash(fs);
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="CityHash32"/>
/// <seealso cref="CityHash64"/>
/// <seealso cref="CityHash128"/>
public abstract class CityHash<T>
    : NonCryptographicHashAlgorithm
    where T : CityHash<T>, new()
{
    /// <summary>The first Murmur-style mixing constant used in 32-bit operations.</summary>
    protected const uint C1 = 0xCC9E2D51U;

    /// <summary>The second Murmur-style mixing constant used in 32-bit operations.</summary>
    protected const uint C2 = 0x1B873593U;

    /// <summary>The 32-bit finalisation magic constant applied during the iterative mixing phase.</summary>
    protected const uint HashMagic = 0xE6546B64U;

    /// <summary>The first 64-bit mixing constant, derived from the CityHash reference implementation.</summary>
    protected const ulong K0 = 0xC3A5C85C97CB3127UL;

    /// <summary>The second 64-bit mixing constant, derived from the CityHash reference implementation.</summary>
    protected const ulong K1 = 0xB492B66FBE98F273UL;

    /// <summary>The third 64-bit mixing constant, derived from the CityHash reference implementation.</summary>
    protected const ulong K2 = 0x9AE16A3B2F90404FUL;

    /// <summary>
    /// The fourth 64-bit constant used by the 128-bit variant to seed the accumulator from the first 16
    /// bytes of input.
    /// </summary>
    protected const ulong K3 = 0xC949D7C7509E6557UL;

    /// <summary>
    /// The prime multiplier used by the 64-bit <see cref="HashLen16(ulong, ulong)" /> finalisation step.
    /// </summary>
    protected const ulong KMul = 0x9DDFEA08EB382D69UL;

    private static readonly int[] ValidHashSizes = { 32, 64, 128 };

    private readonly MemoryStream _inputBuffer = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CityHash{T}" /> class with the specified hash output
    /// size.
    /// </summary>
    /// <param name="hashSize">The desired hash output size in bits. Must be one of 32, 64, or 128.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not one of the supported values.
    /// </exception>
    protected CityHash(int hashSize)
        : base(ValidateHashSize(hashSize) / 8)
    {
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        if (source.Length > 0)
            this._inputBuffer.Write(source);
    }

    /// <inheritdoc />
    public override void Reset()
    {
        this._inputBuffer.SetLength(0);
        this._inputBuffer.Position = 0;
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        // CityHash is a one-shot algorithm; finalisation re-runs over the accumulated buffer so that
        // GetCurrentHash remains non-destructive and may be invoked multiple times.
        byte[] data = this._inputBuffer.ToArray();
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
    /// Applies a final avalanche mixing step to a 32-bit value, improving bit diffusion.
    /// </summary>
    /// <param name="h">The 32-bit value to mix.</param>
    /// <returns>The mixed 32-bit result.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint Mix(uint h) => h ^ (h >> 16);

    /// <summary>
    /// Applies a single Murmur-style multiply-rotate-XOR step, combining two 32-bit values.
    /// </summary>
    /// <param name="a">The input value to multiply and fold.</param>
    /// <param name="h">The accumulator value to combine with the result.</param>
    /// <returns>The result of the Murmur mixing step.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint Mur(uint a, uint h) =>
        unchecked((a * C1) ^ h.RotateBitsRightUnchecked(17));

    /// <summary>
    /// Performs a cyclic three-way permutation of the given values, assigning <c>a ← c</c>, <c>c ← b</c>,
    /// <c>b ← a</c>.
    /// </summary>
    /// <param name="a">The first value, receives the original value of <paramref name="c" />.</param>
    /// <param name="b">The second value, receives the original value of <paramref name="a" />.</param>
    /// <param name="c">The third value, receives the original value of <paramref name="b" />.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void Permute3(ref uint a, ref uint b, ref uint c)
    {
        uint t = a;
        a = c;
        c = b;
        b = t;
    }

    /// <summary>
    /// Applies a final 64-bit entropy spreading step by XOR-ing the value with its own 47-bit right shift.
    /// </summary>
    /// <param name="val">The 64-bit value to mix.</param>
    /// <returns>The mixed 64-bit result.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ulong ShiftMix(ulong val) => val ^ (val >> 47);

    /// <summary>
    /// Combines two 64-bit values into a single 64-bit hash using the default <see cref="KMul" /> multiplier.
    /// </summary>
    /// <param name="u">The first input value.</param>
    /// <param name="v">The second input value.</param>
    /// <returns>The combined 64-bit hash value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ulong HashLen16(ulong u, ulong v) => HashLen16(u, v, KMul);

    /// <summary>
    /// Combines two 64-bit values into a single 64-bit hash using a caller-supplied multiplier, applying
    /// two rounds of multiply-shift-XOR to thoroughly distribute entropy across all output bits.
    /// </summary>
    /// <param name="u">The first input value.</param>
    /// <param name="v">The second input value.</param>
    /// <param name="mul">The multiplier to apply during mixing. Typically a large odd prime.</param>
    /// <returns>The combined 64-bit hash value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ulong HashLen16(ulong u, ulong v, ulong mul)
    {
        ulong a = (u ^ v) * mul;
        a ^= a >> 47;

        ulong b = (v ^ a) * mul;
        b ^= b >> 47;

        return b * mul;
    }

    /// <summary>
    /// Computes a weak 64-bit hash of 32 bytes using six provided seed values, returning a pair of
    /// independent 64-bit outputs.
    /// </summary>
    /// <param name="w">The first 64-bit word of the input block.</param>
    /// <param name="x">The second 64-bit word of the input block.</param>
    /// <param name="y">The third 64-bit word of the input block.</param>
    /// <param name="z">The fourth 64-bit word of the input block.</param>
    /// <param name="a">The first accumulator seed.</param>
    /// <param name="b">The second accumulator seed.</param>
    /// <returns>
    /// A tuple containing two independent 64-bit hash values derived from the mixed input and seeds.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static (ulong First, ulong Second) WeakHashLen32WithSeeds(
        ulong w, ulong x, ulong y, ulong z, ulong a, ulong b)
    {
        a += w;
        b = (b + a + z).RotateBitsRightUnchecked(21);

        ulong c = a;
        a += x;
        a += y;
        b += a.RotateBitsRightUnchecked(44);

        return (a + z, b + c);
    }

    /// <summary>
    /// Reads four consecutive 64-bit little-endian words from the specified span and forwards them to
    /// <see cref="WeakHashLen32WithSeeds(ulong, ulong, ulong, ulong, ulong, ulong)" /> along with the
    /// provided seeds.
    /// </summary>
    /// <param name="s">The input span. Must contain at least 32 bytes starting at offset 0.</param>
    /// <param name="a">The first accumulator seed.</param>
    /// <param name="b">The second accumulator seed.</param>
    /// <returns>
    /// A tuple containing two independent 64-bit hash values derived from the 32-byte block and seeds.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static (ulong First, ulong Second) WeakHashLen32WithSeeds(
        ReadOnlySpan<byte> s, ulong a, ulong b) =>
        WeakHashLen32WithSeeds(
            BinaryPrimitives.ReadUInt64LittleEndian(s),
            BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(8)),
            BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(16)),
            BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(24)),
            a, b);

    /// <summary>
    /// Hashes 0 to 16 bytes to a 64-bit value, selecting a byte-, word-, or double-word code path based on
    /// the exact input length.
    /// </summary>
    /// <param name="s">The input span. Length must be in the range [0, 16].</param>
    /// <returns>The 64-bit hash value.</returns>
    /// <remarks>
    /// An empty input returns <see cref="K2" /> directly.
    /// </remarks>
    protected static ulong Hash64Len0to16(ReadOnlySpan<byte> s)
    {
        int len = s.Length;

        if (len >= 8)
        {
            ulong mul = K2 + (ulong)(len * 2);
            ulong a = BinaryPrimitives.ReadUInt64LittleEndian(s) + K2;
            ulong b = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(len - 8));
            ulong c = b.RotateBitsRightUnchecked(37) * mul + a;
            ulong d = (a.RotateBitsRightUnchecked(25) + b) * mul;
            return HashLen16(c, d, mul);
        }

        if (len >= 4)
        {
            ulong mul = K2 + (ulong)(len * 2);
            ulong a = BinaryPrimitives.ReadUInt32LittleEndian(s);
            return HashLen16((ulong)len + (a << 3), BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(len - 4)), mul);
        }

        if (len > 0)
        {
            byte a = s[0];
            byte b = s[len >> 1];
            byte c = s[len - 1];
            uint y = a + ((uint)b << 8);
            uint z = (uint)len + ((uint)c << 2);
            return ShiftMix(y * K2 ^ z * K0) * K2;
        }

        return K2;
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
