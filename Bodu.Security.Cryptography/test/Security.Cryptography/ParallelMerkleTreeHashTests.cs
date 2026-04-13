using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Bodu.Infrastructure;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Unit tests for <see cref="ParallelMerkleTreeHash"/>.
    /// </summary>
    [TestClass]
    public partial class ParallelMerkleTreeHashTests
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
        /// The algorithm computes an additive hash — the unsigned byte-sum of all input bytes as a
        /// little-endian uint32 — making expected values trivially hand-computable and enabling
        /// direct comparison with the sequential <see cref="MerkleTreeHash"/> implementation.
        /// </summary>
        private static Func<HashAlgorithm> Factory => () => new MonitoringHashAlgorithm();

        // -----------------------------------------------------------------------------------------
        // Shared data helpers
        // -----------------------------------------------------------------------------------------

        /// <summary>Returns a byte array of <paramref name="length"/> bytes with values i % 251.</summary>
        private static byte[] MakeData(int length) => MakeData(length, seed: 1);

        /// <summary>Returns a byte array of <paramref name="length"/> bytes starting at <paramref name="seed"/> modulo 251.</summary>
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
        // These replicate the exact algorithm used by ParallelMerkleTreeHash:
        //   Leaf      → additive hash of the zero-padded block (via TryComputeHash/HashCore span)
        //   Internal  → additive hash of concatenated child hashes (via TransformBlock)
        //
        // Both paths sum the same bytes, so the helper is identical to the sequential one.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Computes the expected Merkle root hash for <paramref name="data"/> using an additive
        /// hash algorithm, exactly matching the zero-padding and tree-reduction strategy of
        /// <see cref="ParallelMerkleTreeHash"/>.
        /// </summary>
        internal static byte[] ComputeAdditiveRoot(byte[] data, int blockSize, int fanOut)
        {
            if (data.Length == 0)
                throw new ArgumentException("Data must be non-empty.", nameof(data));

            var level = new List<byte[]>();
            for (int offset = 0; offset < data.Length; offset += blockSize)
            {
                int len = Math.Min(blockSize, data.Length - offset);
                var block = new byte[blockSize];
                Array.Copy(data, offset, block, 0, len);
                level.Add(AdditiveHash(block));
            }

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

        internal static byte[] AdditiveHash(byte[] block)
        {
            uint sum = 0;
            foreach (byte b in block) sum += b;
            return BitConverter.GetBytes(sum);
        }

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