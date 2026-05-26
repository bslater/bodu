// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.IO.Hashing.Checksums;

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
/// <para>
/// <strong>When to choose Fletcher.</strong> Fletcher was designed as a cheaper alternative to CRC for detecting
/// accidental corruption in network protocols and file formats — TCP, Modbus ASCII, and ZFS all use a Fletcher variant.
/// It catches single-bit errors and many burst errors at a fraction of CRC's per-byte cost, making it attractive on
/// resource-constrained microcontrollers and in tight inner loops. Pick <see cref="Fletcher16" /> for embedded
/// protocols where 16 bits is enough, <see cref="Fletcher32" /> as the workhorse for general file-integrity work, and
/// <see cref="Fletcher64" /> when a wider checksum reduces collision pressure on large datasets. For stronger
/// error-detection guarantees prefer <see cref="Crc" />; for hash-table keying prefer
/// <see cref="Bodu.IO.Hashing.MurmurHash3{T}" /> or <see cref="Bodu.IO.Hashing.CityHash{T}" />, which give better
/// avalanche than any positional-sum scheme.
/// </para>
/// <para>
/// <strong>Lifecycle and threading.</strong> Inherits the standard
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Append(System.ReadOnlySpan{byte})" /> /
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Reset" /> /
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()" /> shape via
/// <see cref="BlockNonCryptographicHashAlgorithm{T}" />. Snapshotting is non-destructive — call <c>GetCurrentHash</c>
/// as often as needed. Instances are not thread-safe; share behind explicit synchronization, or allocate one per
/// consumer.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing.Checksums;
/// using Bodu.IO.Hashing.Extensions;
///
/// 32-bit Fletcher checksum of a packet payload.
/// var fletcher = new Fletcher32();
/// byte[] checksum = fletcher.ComputeHash(payload);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="Fletcher16"/> <seealso cref="Fletcher32"/> <seealso cref="Fletcher64"/> <seealso cref="Crc"/>
public abstract class Fletcher<TSelf>
    : BlockNonCryptographicHashAlgorithm<TSelf>
    where TSelf : Fletcher<TSelf>, new()
{
    private static readonly int[] s_validHashSizes = [16, 32, 64];

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
            hashLengthInBytes: s_validHashSizes.Contains(hashSize)
                ? hashSize / 8
                : throw new ArgumentException(
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        HashingResourceStrings.Arg_OutOfRange_HashSize,
                        hashSize,
                        string.Join(", ", s_validHashSizes)),
                    nameof(hashSize)),
            blockSize: 1)
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
    protected override TSelf Clone()
    {
        var clone = new TSelf
        {
            _partA = _partA,
            _partB = _partB,
        };
        clone.CopyResidualStateFrom(this);
        return clone;
    }

    /// <inheritdoc />
    protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
    {
        var buffer = new byte[BlockSizeBytes];
        block.CopyTo(buffer);
        return buffer;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        _partA = (_partA + block[0]) % _modulus;
        _partB = (_partB + _partA) % _modulus;
    }

    /// <inheritdoc />
    protected override byte[] ProcessFinalBlock()
    {
        var result = new byte[_hashSizeBits / 8];
        var halfLength = result.Length / 2;

        WriteBigEndian(_partB, result.AsSpan(0, halfLength));
        WriteBigEndian(_partA, result.AsSpan(halfLength, halfLength));

        return result;
    }

    /// <inheritdoc />
    protected override void ResetState()
    {
        _partA = 0;
        _partB = 0;
    }

    /// <inheritdoc />
    protected override bool ShouldPadFinalBlock() => false;

    private static void WriteBigEndian(ulong value, Span<byte> destination)
    {
        for (var i = 0; i < destination.Length; i++)
        {
            destination[i] = (byte)(value >> ((destination.Length - i - 1) << 3));
        }
    }
}
