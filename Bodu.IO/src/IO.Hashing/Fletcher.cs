// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Bodu.Extensions;

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

    private readonly int hashSizeBits;
    private readonly ulong modulus;

    private ulong partA;
    private ulong partB;

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
        this.hashSizeBits = hashSize;
        this.modulus = (1UL << (hashSize / 2)) - 1;
        this.AlgorithmName = $"Fletcher-{hashSize}";
    }

    /// <summary>
    /// Gets the algorithm name in the form <c>Fletcher-N</c>, where <c>N</c> is the output width in bits.
    /// </summary>
    /// <value>A string such as <c>Fletcher-16</c>, <c>Fletcher-32</c>, or <c>Fletcher-64</c>.</value>
    public string AlgorithmName { get; }

    /// <inheritdoc />
    protected override void ResetState()
    {
        this.partA = 0;
        this.partB = 0;
    }

    /// <inheritdoc />
    protected override TSelf Clone()
    {
        TSelf clone = new();
        clone.partA = this.partA;
        clone.partB = this.partB;
        clone.CopyResidualStateFrom(this);
        return clone;
    }

    /// <inheritdoc />
    protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
    {
        byte[] buffer = new byte[this.BlockSizeBytes];
        block.CopyTo(buffer);
        return buffer;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        ulong b = 0;
        for (int i = 0; i < block.Length && i < this.BlockSizeBytes; i++)
        {
            b |= ((ulong)block[i]) << ((this.BlockSizeBytes - (i + 1)) << 3);
        }

        this.partA = (this.partA + b) % this.modulus;
        this.partB = (this.partB + this.partA) % this.modulus;
    }

    /// <inheritdoc />
    protected override byte[] ProcessFinalBlock()
    {
        ulong finalHash = (this.partA << (this.hashSizeBits / 2)) | this.partB;
        return finalHash.GetBytes().SliceInternal(0, this.hashSizeBits / 8);
    }

    /// <inheritdoc />
    protected override bool ShouldPadFinalBlock() => false;
}
