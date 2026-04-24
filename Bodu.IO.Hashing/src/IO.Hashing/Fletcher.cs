// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

using Bodu.Extensions;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

/// <summary>
/// Provides a base class for the Fletcher checksum family (Fletcher-16, Fletcher-32, Fletcher-64).
/// </summary>
/// <typeparam name="TSelf">The concrete derived type (CRTP) used for block-hash reuse.</typeparam>
/// <remarks>
/// <para>
/// Fletcher is a non-cryptographic position-dependent checksum that maintains two running accumulators (A and B) and
/// combines them into the final hash. Derived types <see cref="Fletcher16" />, <see cref="Fletcher32" />, and
/// <see cref="Fletcher64" /> select the output width.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password
/// hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public abstract class Fletcher<TSelf>
    : BlockNonCryptographicHashAlgorithm<TSelf>
    where TSelf : Fletcher<TSelf>, new()
{
    private static readonly int[] ValidHashSizes = { 16, 32, 64 };

    private readonly int _hashSizeBits;
    private readonly ulong _modulus;

    private ulong _partA;
    private ulong _partB;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fletcher{TSelf}" /> class with the specified hash size.
    /// </summary>
    /// <param name="hashSize">The hash size in bits. Valid values are 16, 32, or 64.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="hashSize" /> is not 16, 32, or 64.</exception>
    protected Fletcher(int hashSize)
        : base(
            hashLengthInBytes: ValidHashSizes.Contains(hashSize)
                ? hashSize / 8
                : throw new ArgumentException(
                    string.Format(
                        "Invalid hash size: {0}. Valid sizes are: {1}.",
                        hashSize,
                        string.Join(", ", ValidHashSizes)),
                    nameof(hashSize)),
            blockSize: hashSize / 16)
    {
        _hashSizeBits = hashSize;
        _modulus = (1UL << (hashSize / 2)) - 1;
        AlgorithmName = $"Fletcher-{hashSize}";
    }

    /// <summary>
    /// Gets the algorithm name in the form <c>Fletcher-N</c>, where <c>N</c> is the output width in bits.
    /// </summary>
    /// <value>A string such as <c>Fletcher-16</c>, <c>Fletcher-32</c>, or <c>Fletcher-64</c>.</value>
    public string AlgorithmName { get; }

    /// <inheritdoc />
    protected override void ResetState()
    {
        _partA = 0;
        _partB = 0;
    }

    /// <inheritdoc />
    protected override TSelf Clone()
    {
        TSelf clone = new();
        clone._partA = _partA;
        clone._partB = _partB;
        clone.CopyResidualStateFrom(this);
        return clone;
    }

    /// <inheritdoc />
    protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
    {
        byte[] buffer = new byte[BlockSizeBytes];
        block.CopyTo(buffer);
        return buffer;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        ulong b = 0;
        for (int i = 0; i < block.Length && i < BlockSizeBytes; i++)
        {
            b |= ((ulong)block[i]) << ((BlockSizeBytes - (i + 1)) << 3);
        }

        _partA = (_partA + b) % _modulus;
        _partB = (_partB + _partA) % _modulus;
    }

    /// <inheritdoc />
    protected override byte[] ProcessFinalBlock()
    {
        ulong finalHash = (_partA << (_hashSizeBits / 2)) | _partB;
        return finalHash.GetBytes().SliceInternal(0, _hashSizeBits / 8);
    }

    /// <inheritdoc />
    protected override bool ShouldPadFinalBlock() => false;
}
