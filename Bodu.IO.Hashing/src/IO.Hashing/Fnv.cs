// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Bodu.IO.Hashing;

/// <summary>
/// Provides a base class for the Fowler-Noll-Vo (FNV) hash family, supporting both the FNV-1 and FNV-1a
/// variants at 32-bit and 64-bit widths.
/// </summary>
/// <typeparam name="TSelf">The concrete derived type. Must expose a public parameterless constructor.</typeparam>
/// <remarks>
/// <para>
/// FNV maintains a running hash initialised from an <c>offset basis</c> and processes each input byte by
/// combining multiplication with a large <c>FNV prime</c> and a bitwise XOR. Derived types select the width
/// and variant:
/// </para>
/// <list type="bullet">
/// <item><term>FNV-1</term><description>multiplication followed by XOR.</description></item>
/// <item><term>FNV-1a</term><description>XOR followed by multiplication.</description></item>
/// </list>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public abstract class Fnv<TSelf>
    : NonCryptographicHashAlgorithm
    where TSelf : Fnv<TSelf>, new()
{
    private static readonly int[] ValidHashSizes = { 32, 64 };

    private readonly int _hashSizeBits;
    private readonly ulong _offsetBasis;
    private readonly ulong _prime;
    private readonly bool _useFnv1a;

    private ulong _workingHash;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fnv{TSelf}" /> class using the specified configuration
    /// parameters.
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
    /// Gets the algorithm name in the form <c>FNV-{variant}-{bits}</c>, e.g. <c>FNV-1-32</c> or
    /// <c>FNV-1a-64</c>.
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

    private static int ValidateHashSize(int hashSize)
    {
        if (Array.IndexOf(ValidHashSizes, hashSize) == -1)
        {
            throw new ArgumentException(
                $"Invalid hash size: {hashSize}. Valid sizes are: {string.Join(", ", ValidHashSizes)}.",
                nameof(hashSize));
        }

        return hashSize;
    }

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
