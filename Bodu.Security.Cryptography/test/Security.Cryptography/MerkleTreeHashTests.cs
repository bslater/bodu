using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Bodu.Infrastructure;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Unit tests for <see cref="MerkleTreeHash"/>.
    /// </summary>
    [TestClass]
    public partial class MerkleTreeHashTests
    {
        public TestContext TestContext { get; set; } = null!;

        // -----------------------------------------------------------------------------------------
        // Shared constants
        // -----------------------------------------------------------------------------------------

        private const int DefaultBlockSize = 4;
        private const int DefaultFanOut = 2;

        // -----------------------------------------------------------------------------------------
        // Shared factory
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Returns a factory producing fresh <see cref="MonitoringHashAlgorithm"/> instances.
        /// The algorithm computes an additive hash — the unsigned byte-sum of all input as a
        /// little-endian uint32 — making expected values trivially hand-computable.
        /// </summary>
        private static Func<HashAlgorithm> Factory => () => new MonitoringHashAlgorithm();

        // -----------------------------------------------------------------------------------------
        // Shared data helpers
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Returns a byte array of <paramref name="length"/> bytes with values 1, 2, 3, … repeating
        /// at 251 (a prime, avoiding block-boundary alignment).
        /// </summary>
        private static byte[] MakeData(int length) =>
            MakeData(length, seed: 1);

        /// <summary>
        /// Returns a byte array of <paramref name="length"/> bytes starting at <paramref name="seed"/>
        /// and incrementing modulo 251.
        /// </summary>
        private static byte[] MakeData(int length, int seed)
        {
            var data = new byte[length];
            for (int i = 0; i < length; i++)
                data[i] = (byte)((seed + i) % 251);
            return data;
        }

        // -----------------------------------------------------------------------------------------
        // Hand-computation helpers
        //
        // These replicate the exact algorithm used by MerkleTreeHash:
        //   Leaf  → additive hash of the zero-padded block bytes
        //   Internal → additive hash of the concatenated child-hash bytes
        //
        // For the MonitoringHashAlgorithm the result is BitConverter.GetBytes((uint)byteSum).
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Computes the expected Merkle root hash for <paramref name="data"/> using an additive
        /// hash algorithm, matching the exact zero-padding and tree-reduction strategy of
        /// <see cref="MerkleTreeHash"/>.
        /// </summary>
        internal static byte[] ComputeAdditiveRoot(byte[] data, int blockSize, int fanOut)
        {
            if (data.Length == 0)
                throw new ArgumentException("Data must be non-empty.", nameof(data));

            // Build leaf hashes, zero-padding the final partial block.
            var level = new List<byte[]>();
            for (int offset = 0; offset < data.Length; offset += blockSize)
            {
                int len = Math.Min(blockSize, data.Length - offset);
                var block = new byte[blockSize];
                Array.Copy(data, offset, block, 0, len);
                level.Add(AdditiveHash(block));
            }

            // Reduce level by level.
            while (level.Count > 1)
            {
                var next = new List<byte[]>();
                for (int i = 0; i < level.Count; i += fanOut)
                {
                    int groupSize = Math.Min(fanOut, level.Count - i);
                    var group = level.GetRange(i, groupSize);
                    next.Add(AdditiveHashConcat(group));
                }
                level = next;
            }

            return level[0];
        }

        /// <summary>
        /// Computes the additive hash of <paramref name="block"/>: unsigned sum of all bytes as
        /// a little-endian uint32.
        /// </summary>
        internal static byte[] AdditiveHash(byte[] block)
        {
            uint sum = 0;
            foreach (byte b in block) sum += b;
            return BitConverter.GetBytes(sum);
        }

        /// <summary>
        /// Computes the additive hash of the concatenation of all bytes in <paramref name="hashes"/>,
        /// replicating the TransformBlock / TransformFinalBlock combination strategy.
        /// </summary>
        internal static byte[] AdditiveHashConcat(List<byte[]> hashes)
        {
            uint sum = 0;
            foreach (var h in hashes)
                foreach (byte b in h)
                    sum += b;
            return BitConverter.GetBytes(sum);
        }
    }
}