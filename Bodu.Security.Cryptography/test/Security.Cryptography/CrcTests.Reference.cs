// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.Reference.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Catalogue-wide parity tests: for every <see cref="CrcStandard" /> in <see cref="CrcStandard.All" />,
    /// drive the production <see cref="Crc" /> against the bit-by-bit <see cref="CrcReference" /> using a
    /// battery of shared input vectors and assert byte-identical output.
    /// </summary>
    public partial class CrcTests
    {
        /// <summary>
        /// Shared input vectors exercised by the reference-parity tests. Deliberately mixes very short inputs
        /// (which stress bit-level reflection), byte-aligned inputs, and longer runs that span the lookup
        /// table's indexing path.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, byte[]> ReferenceInputs = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["Empty"]         = Array.Empty<byte>(),
            ["SingleZero"]    = new byte[] { 0x00 },
            ["SingleFF"]      = new byte[] { 0xFF },
            ["Check"]         = Encoding.ASCII.GetBytes("123456789"),
            ["ABC"]           = Encoding.ASCII.GetBytes("ABC"),
            ["QuickBrownFox"] = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog"),
            ["Sequential_16"] = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(),
            ["Sequential_64"] = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray(),
            ["RandomDeterministic_200"] = GenerateDeterministicBytes(seed: 0xC0FFEEu, count: 200),
        };

        /// <summary>
        /// Verifies that <see cref="Crc" /> produces the same output as <see cref="CrcReference" /> for every
        /// input in <see cref="ReferenceInputs" /> under the supplied catalogue <paramref name="standard" />.
        /// </summary>
        /// <param name="standard">The catalogue <see cref="CrcStandard" /> under test.</param>
        [TestMethod]
        [DynamicData(nameof(CatalogStandards), DynamicDataSourceType.Method)]
        public void ComputeHash_ForCatalogStandard_ShouldMatchReferenceImplementation(CrcStandard standard)
        {
            foreach (var (inputName, input) in ReferenceInputs)
            {
                ulong expected = CrcReference.Compute(standard, input);

                using var crc = new Crc(standard);
                byte[] hash = crc.ComputeHash(input);

                Assert.AreEqual(
                    (standard.Size + 7) / 8,
                    hash.Length,
                    $"Hash length mismatch for {standard.Name}: expected {(standard.Size + 7) / 8} bytes, got {hash.Length}.");

                ulong actual = CrcReference.PackLittleEndian(hash);

                Assert.AreEqual(
                    expected,
                    actual,
                    $"{standard.Name} on input '{inputName}' ({input.Length} bytes): expected 0x{expected:X}, got 0x{actual:X}.");
            }
        }

        /// <summary>
        /// Verifies that streaming the same input in single-byte chunks via <see cref="Crc.HashCore(byte[], int, int)" />
        /// / <see cref="Crc.TransformBlock(byte[], int, int, byte[], int)" /> yields the same result as a single
        /// <see cref="Crc.ComputeHash(byte[])" /> call, across every catalogue standard.
        /// </summary>
        /// <param name="standard">The catalogue <see cref="CrcStandard" /> under test.</param>
        [TestMethod]
        [DynamicData(nameof(CatalogStandards), DynamicDataSourceType.Method)]
        public void ComputeHash_WhenStreamedInOneByteChunks_ForCatalogStandard_ShouldMatchSingleCall(CrcStandard standard)
        {
            byte[] input = ReferenceInputs["RandomDeterministic_200"];

            using var oneShot = new Crc(standard);
            byte[] expected = oneShot.ComputeHash(input);

            using var streamed = new Crc(standard);
            foreach (byte b in input)
                streamed.TransformBlock(new[] { b }, 0, 1, null, 0);
            streamed.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            byte[] actual = streamed.Hash!;

            CollectionAssert.AreEqual(expected, actual, $"Streaming parity failed for {standard.Name}.");
        }

        private static byte[] GenerateDeterministicBytes(uint seed, int count)
        {
            // Simple xorshift32 — self-contained, deterministic across platforms, independent of System.Random.
            byte[] buffer = new byte[count];
            uint state = seed == 0u ? 1u : seed;
            for (int i = 0; i < count; i++)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                buffer[i] = (byte)(state & 0xFFu);
            }
            return buffer;
        }
    }
}
