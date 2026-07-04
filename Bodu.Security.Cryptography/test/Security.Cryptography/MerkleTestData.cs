// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTestData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Shared test fixtures, data generators, and hand-computation helpers used by both
/// <see cref="MerkleTreeHashTests" /> and <see cref="ParallelMerkleTreeHashTests" />.
/// </summary>
/// <remarks>
/// <para>
/// This class centralises every piece of boilerplate that would otherwise be duplicated across
/// the two test hierarchies — the algorithm factory, data generators, default configuration
/// constants, and the reference implementation of the Merkle reduction used to compute
/// expected root hashes by hand.
/// </para>
/// <para>
/// The <see cref="Factory" /> produces <see cref="MonitoringHashAlgorithm" /> instances, which
/// compute an additive hash — the unsigned sum of all input bytes as a little-endian
/// <see cref="uint" />. This choice makes expected values hand-computable and keeps the tests
/// independent of any real cipher. The trade-off is that an additive hash is commutative, so
/// tests that need to catch ordering bugs should use a real non-commutative algorithm (see
/// <c>MerkleTreeHashTestsBase.RealAlgorithm.cs</c>).
/// </para>
/// </remarks>
internal static class MerkleTestData
{
    /// <summary>Default block size used by the shared base-class tests.</summary>
    internal const int DefaultBlockSize = 4;

    /// <summary>Default fan-out used by the shared base-class tests.</summary>
    internal const int DefaultFanOut = 2;

    /// <summary>
    /// Gets a factory that produces fresh <see cref="MonitoringHashAlgorithm" /> instances.
    /// </summary>
    /// <remarks>
    /// The algorithm computes an additive hash — the unsigned byte-sum of all input bytes as a
    /// little-endian <see cref="uint" /> — making expected values trivially hand-computable.
    /// </remarks>
    internal static Func<HashAlgorithm> Factory => () => new MonitoringHashAlgorithm();

    /// <summary>
    /// Returns a byte array of <paramref name="length" /> bytes with values 1, 2, 3, …
    /// repeating modulo 251 (a prime — chosen to avoid block-boundary alignment patterns).
    /// </summary>
    /// <param name="length">The number of bytes to generate.</param>
    internal static byte[] MakeData(int length) => MakeData(length, seed: 1);

    /// <summary>
    /// Returns a byte array of <paramref name="length" /> bytes starting at
    /// <paramref name="seed" /> and incrementing modulo 251.
    /// </summary>
    /// <param name="length">The number of bytes to generate.</param>
    /// <param name="seed">The starting byte value for the sequence.</param>
    internal static byte[] MakeData(int length, int seed)
    {
        byte[] data = new byte[length];
        for (int i = 0; i < length; i++)
            data[i] = (byte)((seed + i) % 251);
        return data;
    }

    /// <summary>
    /// Computes the expected Merkle root hash for <paramref name="data" /> using an additive
    /// hash, exactly replicating the zero-padding and tree-reduction strategy used by both
    /// <see cref="MerkleTreeHash" /> and <see cref="ParallelMerkleTreeHash" />.
    /// </summary>
    /// <param name="data">The raw input bytes to hash. Must not be empty.</param>
    /// <param name="blockSize">
    /// The number of bytes per leaf block. Partial final blocks are zero-padded to this length.
    /// </param>
    /// <param name="fanOut">The maximum number of child nodes combined into each parent node.</param>
    /// <returns>The Merkle root hash as a 4-byte little-endian <see cref="uint" />.</returns>
    /// <exception cref="ArgumentException"><paramref name="data" /> is empty.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>
    /// <b>Leaf nodes</b> — each input chunk is hashed as <c>H(0x00 || chunk)</c> at its actual length (the partial
    /// tail is not zero-padded), matching the implementation's leaf path.
    /// </description></item>
    /// <item><description>
    /// <b>Internal nodes</b> — child hashes within a fan-out group are hashed as <c>H(0x01 || child₀ || … )</c>,
    /// matching the implementation's <c>CombineAndHash</c> / streaming <c>TransformBlock</c> path.
    /// </description></item>
    /// </list>
    /// <para>
    /// The additive hash is commutative and blind to the <c>0x00</c> leaf prefix and to dropped zero padding (both
    /// add 0 to the byte-sum); it is sensitive only to the <c>0x01</c> internal-node prefix, which adds 1 per node.
    /// Order- and format-sensitive coverage lives in the SHA-256 tests
    /// (<c>MerkleTreeHashTestsBase{T}.RealAlgorithm.cs</c> and the domain-separation partial).
    /// </para>
    /// </remarks>
    internal static byte[] ComputeAdditiveRoot(byte[] data, int blockSize, int fanOut)
    {
        if (data.Length == 0)
            throw new ArgumentException("Data must be non-empty.", nameof(data));

        // Level 0: one leaf hash per block. The partial tail block is hashed at its actual length (no padding).
        var level = new List<byte[]>();
        for (int offset = 0; offset < data.Length; offset += blockSize)
        {
            int len = Math.Min(blockSize, data.Length - offset);
            level.Add(AdditiveHash(data.AsSpan(offset, len)));
        }

        // Reduce each level by grouping nodes in slices of fanOut until only the root remains.
        // The final group in a level may be partial when node count is not a multiple of fanOut.
        while (level.Count > 1)
        {
            var next = new List<byte[]>();
            for (int i = 0; i < level.Count; i += fanOut)
            {
                int groupSize = Math.Min(fanOut, level.Count - i);
                next.Add(AdditiveHashConcat(level.GetRange(i, groupSize)));
            }
            level = next;
        }

        return level[0];
    }

    /// <summary>
    /// Returns the additive leaf hash <c>H(0x00 || block)</c> as a 4-byte little-endian
    /// <see cref="uint" /> — matching the length-bound, domain-prefixed leaf path in both implementations.
    /// </summary>
    /// <param name="block">The actual-length leaf bytes (not zero-padded).</param>
    internal static byte[] AdditiveHash(ReadOnlySpan<byte> block)
    {
        // Leaf-domain prefix contributes 0x00 → adds nothing to the byte-sum.
        uint sum = MerkleTreeFormat.LeafPrefix;
        foreach (byte b in block)
            sum += b;
        return BitConverter.GetBytes(sum);
    }

    /// <summary>
    /// Returns the additive internal-node hash <c>H(0x01 || child₀ || … )</c> as a 4-byte little-endian
    /// <see cref="uint" /> — matching the domain-prefixed internal-node hashing path.
    /// </summary>
    /// <param name="hashes">The child hashes to combine, in order.</param>
    /// <remarks>
    /// The additive hash is commutative across input boundaries, so iterating the flat byte
    /// sequence produces the same result as streaming each child via separate
    /// <see cref="HashAlgorithm.TransformBlock" /> calls. The internal-node prefix contributes 0x01 → adds 1.
    /// </remarks>
    internal static byte[] AdditiveHashConcat(List<byte[]> hashes)
    {
        uint sum = MerkleTreeFormat.InternalNodePrefix;
        foreach (byte[] h in hashes)
            foreach (byte b in h)
                sum += b;
        return BitConverter.GetBytes(sum);
    }
}
