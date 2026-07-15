// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Bodu.IO.Hashing;

/// <summary>
/// Provides a base class for the Fowler-Noll-Vo (FNV) hash family, supporting both the FNV-1 and FNV-1a variants at
/// 32-bit and 64-bit widths.
/// </summary>
/// <typeparam name="TSelf">The concrete derived type. Must expose a public parameterless constructor.</typeparam>
/// <remarks>
/// <para>
/// FNV maintains a running hash initialized from an <c>offset basis</c> and processes each input byte by combining
/// multiplication with a large <c>FNV prime</c> and a bitwise XOR. Derived types select the width and variant:
/// </para>
/// <list type="bullet">
/// <item>
/// <term>FNV-1</term>
/// <description>multiplication followed by XOR.</description>
/// </item>
/// <item>
/// <term>FNV-1a</term>
/// <description>XOR followed by multiplication.</description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose FNV.</strong> FNV is byte-at-a-time, allocation-free, and trivially fast on small inputs — a
/// common choice for hashing identifiers, dictionary keys, and cache lookups in hot paths. For most new code prefer
/// <see cref="Fnv1a32" /> or <see cref="Fnv1a64" />: the FNV-1a ordering has measurably better avalanche than the
/// original FNV-1. For inputs longer than a few hundred bytes, <see cref="MurmurHash3{T}" /> or
/// <see cref="CityHash{T}" /> generally distribute better and are faster on modern CPUs; FNV's strength is its
/// simplicity and predictable performance on short keys.
/// </para>
/// <para>
/// <strong>Output size and lifecycle.</strong> The digest length is fixed by the constructor's <c>hashSize</c> argument
/// (32 or 64 bits) and emitted in big-endian byte order.
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()" /> is non-destructive and may be called
/// any number of times during a running hash; <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Reset" />
/// returns the running state to its initial offset basis.
/// </para>
/// <para>
/// <strong>Thread safety.</strong> Instances are not thread-safe; share behind explicit synchronization, or allocate
/// one per consumer.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Extensions;
///
/// // Pick a width and variant; FNV-1a is the recommended default.
/// var fnv = new Fnv1a64();
/// byte[] digest = fnv.ComputeHash(System.Text.Encoding.UTF8.GetBytes("user@example.com"));
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="Fnv132"/> <seealso cref="Fnv1a32"/> <seealso cref="Fnv164"/> <seealso cref="Fnv1a64"/>
public abstract class Fnv<TSelf>
    : NonCryptographicHashAlgorithm
    where TSelf : Fnv<TSelf>, new()
{
    /// <summary>The set of hash sizes, in bits, accepted by the constructor.</summary>
    private static readonly int[] s_validHashSizes = [32, 64];

    /// <summary>The configured digest length, in bits (32 or 64), used to select how the running hash is emitted.</summary>
    private readonly int _hashSizeBits;

    /// <summary>The initial offset basis used to seed the running hash and restore it on reset.</summary>
    private readonly ulong _offsetBasis;

    /// <summary>The FNV prime multiplier applied to the running hash for each input byte.</summary>
    private readonly ulong _prime;

    /// <summary>Indicates whether the FNV-1a ordering (XOR then multiply) is used; otherwise the FNV-1 ordering (multiply then XOR).</summary>
    private readonly bool _useFnv1a;

    /// <summary>The running hash accumulator, updated as each input byte is folded in and seeded from the offset basis.</summary>
    private ulong _workingHash;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fnv{TSelf}" /> class using the specified configuration parameters.
    /// </summary>
    /// <param name="hashSize">The size, in bits, of the resulting hash value. Supported values are 32 and 64.</param>
    /// <param name="prime">The FNV prime multiplier used during hash computation.</param>
    /// <param name="offsetBasis">The initial offset basis used to seed the running hash.</param>
    /// <param name="useFnv1a">
    /// <see langword="true" /> to use the FNV-1a variant (XOR followed by multiplication); otherwise
    /// <see langword="false" /> to use the FNV-1 variant (multiplication followed by XOR).
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="hashSize" /> is not a supported value (32 or 64).
    /// </exception>
    protected Fnv(int hashSize, ulong prime, ulong offsetBasis, bool useFnv1a)
        : base(
            hashLengthInBytes: ValidateHashSize(hashSize) / 8)
    {
        _hashSizeBits = hashSize;
        _prime = prime;
        _offsetBasis = offsetBasis;
        _useFnv1a = useFnv1a;
        _workingHash = offsetBasis;
        AlgorithmName = $"FNV-{(useFnv1a ? "1a" : "1")}-{hashSize}";
    }

    /// <summary>
    /// Gets the algorithm name in the form <c>FNV-{variant}-{bits}</c>, e.g. <c>FNV-1-32</c> or <c>FNV-1a-64</c>.
    /// </summary>
    public string AlgorithmName { get; }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        if (_useFnv1a)
            AppendFnv1a(source);
        else
            AppendFnv1(source);
    }

    /// <inheritdoc />
    public override void Reset() => _workingHash = _offsetBasis;

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, _workingHash);

        switch (_hashSizeBits)
        {
            case 32:
                buffer.Slice(4, 4).CopyTo(destination);
                break;
            case 64:
                buffer.CopyTo(destination);
                break;
        }
    }

    /// <summary>
    /// Validates that <paramref name="hashSize" /> is one of the FNV hash sizes this implementation supports.
    /// </summary>
    /// <param name="hashSize">The requested hash size, in bits.</param>
    /// <returns>The validated <paramref name="hashSize" />.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="hashSize" /> is not one of the supported hash sizes.
    /// </exception>
    private static int ValidateHashSize(int hashSize)
    {
        if (Array.IndexOf(s_validHashSizes, hashSize) == -1)
        {
            throw new ArgumentException(
                string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    HashingResourceStrings.Arg_OutOfRange_HashSize,
                    hashSize,
                    string.Join(", ", s_validHashSizes)),
                nameof(hashSize));
        }

        return hashSize;
    }

    /// <summary>
    /// Folds the supplied bytes into the running hash using the FNV-1 ordering: multiply by the prime, then XOR the
    /// byte.
    /// </summary>
    /// <param name="source">The bytes to incorporate into the hash.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendFnv1(ReadOnlySpan<byte> source)
    {
        ulong hash = _workingHash;
        ulong prime = _prime;
        for (int i = 0; i < source.Length; i++)
        {
            hash *= prime;
            hash ^= source[i];
        }

        _workingHash = hash;
    }

    /// <summary>
    /// Folds the supplied bytes into the running hash using the FNV-1a ordering: XOR the byte, then multiply by the
    /// prime.
    /// </summary>
    /// <param name="source">The bytes to incorporate into the hash.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendFnv1a(ReadOnlySpan<byte> source)
    {
        ulong hash = _workingHash;
        ulong prime = _prime;
        for (int i = 0; i < source.Length; i++)
        {
            hash ^= source[i];
            hash *= prime;
        }

        _workingHash = hash;
    }
}
