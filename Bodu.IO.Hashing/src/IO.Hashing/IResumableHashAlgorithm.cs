// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IResumableHashAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

using System;
using System.IO.Hashing;

/// <summary>
/// Marks a non-cryptographic hash algorithm whose internal state can be reconstructed from a previously emitted digest,
/// so additional input can be folded into an existing hash <em>without</em> re-reading the bytes that produced it.
/// </summary>
/// <remarks>
/// <para>
/// Many simple, additive non-cryptographic hashes — CRC, FNV, Jenkins-style mixes — are <em>linear</em> in the sense
/// that the running accumulator at any point is fully determined by the bytes seen so far. The published digest hides
/// that accumulator behind a finalisation step (typically a final XOR, output reflection, or width mask), but the step
/// is reversible. Implementing <see cref="IResumableHashAlgorithm"/> declares that an algorithm supports that reverse
/// step and exposes a stable contract for callers who want to incrementally hash a stream that arrives in chunks
/// across process boundaries — a common pattern in tail-anchored log files, content-addressed stores, or streaming
/// integrity checks where the running state is not in memory but the previous digest <em>is</em>.
/// </para>
/// <para>
/// <strong>Usage shape.</strong> Pair the previous digest with the new bytes:
/// </para>
/// <list type="bullet">
///   <item>
///     <term><see cref="ComputeHashFrom(System.ReadOnlySpan{byte}, System.ReadOnlySpan{byte})"/></term>
///     <description>Returns a freshly allocated digest representing
///     <c>hash(previousInput || newData)</c>. The two array overloads are conveniences for callers without spans.</description>
///   </item>
///   <item>
///     <term><see cref="TryComputeHashFrom(System.ReadOnlySpan{byte}, System.ReadOnlySpan{byte}, System.Span{byte}, out int)"/></term>
///     <description>The allocation-free variant that writes into a caller-supplied buffer; returns
///     <see langword="false"/> when the destination is too small, leaving <c>bytesWritten</c> at 0.</description>
///   </item>
/// </list>
/// <para>
/// <strong>Implementation contract.</strong> Implementations must (a) accept any byte sequence of length
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.HashLengthInBytes"/> as a valid <c>previousHash</c>; (b)
/// reverse any final XOR, reflection, or width mask before resuming; (c) re-apply finalisation before returning the
/// new digest; and (d) leave the algorithm instance in a clean state on return. The contract is not preserved across
/// algorithms — a digest produced by <c>CRC-32/ISO-HDLC</c> can only be resumed by an instance configured with the
/// same <see cref="Bodu.IO.Hashing.Checksums.CrcStandard"/>.
/// </para>
/// <para>
/// <strong>When this is the wrong tool.</strong> Cryptographic hashes (SHA-2, SHA-3, BLAKE) are <em>not</em> resumable
/// in this sense — their finalisation collapses internal state irreversibly, which is precisely the property that
/// makes them cryptographically useful. For appending to an in-memory algorithm instance, prefer the standard
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Append(System.ReadOnlySpan{byte})"/> path.
/// <see cref="IResumableHashAlgorithm"/> only earns its keep when the running accumulator has already been discarded
/// and only the digest remains.
/// </para>
/// <example>
/// <code language="csharp">
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Checksums;
///
/// // 1. Compute a baseline digest of a file's first segment.
/// var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
/// crc.Append(File.ReadAllBytes("part-1.bin"));
/// byte[] digest1 = crc.GetCurrentHash();
///
/// // 2. Hours (or processes) later, fold an additional segment into the same digest using only `digest1`.
/// var resumable = (IResumableHashAlgorithm)new Crc(CrcStandard.CRC32_ISOHDLC);
/// byte[] digest2 = resumable.ComputeHashFrom(digest1, File.ReadAllBytes("part-2.bin"));
///
/// // 3. Same result as if both segments had been appended in one session — without holding the running state.
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="System.IO.Hashing.NonCryptographicHashAlgorithm"/>
/// <seealso cref="Bodu.IO.Hashing.Checksums.Crc"/>
public interface IResumableHashAlgorithm
{
    /// <summary>
    /// Resumes a hash computation from a previously finalised hash value, processes additional input, and writes the new
    /// finalised hash to the specified destination span.
    /// </summary>
    /// <param name="previousHash">The previously finalised hash value to resume from.</param>
    /// <param name="newData">The additional input data to include in the resumed hash calculation.</param>
    /// <param name="destination">The destination buffer to write the finalised hash value to.</param>
    /// <param name="bytesWritten">Outputs the number of bytes written to the destination buffer.</param>
    /// <returns>
    /// <see langword="true" /> if the resumed and finalised hash was written successfully; otherwise,
    /// <see langword="false" /> if the destination span was too small.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the <paramref name="previousHash" /> length does not match
    /// <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" />.
    /// </exception>
    bool TryComputeHashFrom(
        ReadOnlySpan<byte> previousHash,
        ReadOnlySpan<byte> newData,
        Span<byte> destination,
        out int bytesWritten);

    /// <summary>
    /// Resumes a hash computation from a previously finalised hash value and processes additional input, returning the
    /// new finalised hash result as a byte array.
    /// </summary>
    /// <param name="previousHash">The previously finalised hash value to resume from.</param>
    /// <param name="newData">The additional input data to include in the resumed hash calculation.</param>
    /// <returns>A byte array containing the new finalised hash result.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the <paramref name="previousHash" /> length does not match
    /// <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" />.
    /// </exception>
    byte[] ComputeHashFrom(ReadOnlySpan<byte> previousHash, ReadOnlySpan<byte> newData);

    /// <summary>
    /// Resumes a hash computation from a previously finalised hash value and processes additional input, returning the
    /// new finalised hash result as a byte array.
    /// </summary>
    /// <param name="previousHash">The previously finalised hash value to resume from. Must not be <see langword="null" />.</param>
    /// <param name="newData">The additional input data to include in the resumed hash calculation. Must not be <see langword="null" />.</param>
    /// <returns>A byte array containing the new finalised hash result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="previousHash" /> or <paramref name="newData" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the <paramref name="previousHash" /> length does not match
    /// <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" />.
    /// </exception>
    byte[] ComputeHashFrom(byte[] previousHash, byte[] newData);

    /// <summary>
    /// Resumes a hash computation from a previously finalised hash value and processes a specified range of new data,
    /// returning the new finalised hash result as a byte array.
    /// </summary>
    /// <param name="previousHash">The previously finalised hash value to resume from. Must not be <see langword="null" />.</param>
    /// <param name="newData">The buffer containing additional input data. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based offset into <paramref name="newData" /> at which to begin reading data.</param>
    /// <param name="length">The number of bytes to read from <paramref name="newData" />.</param>
    /// <returns>A byte array containing the new finalised hash result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="previousHash" /> or <paramref name="newData" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the <paramref name="previousHash" /> length does not match
    /// <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" />, or if the offset and length exceed the bounds of
    /// <paramref name="newData" />.
    /// </exception>
    byte[] ComputeHashFrom(byte[] previousHash, byte[] newData, int offset, int length);
}
