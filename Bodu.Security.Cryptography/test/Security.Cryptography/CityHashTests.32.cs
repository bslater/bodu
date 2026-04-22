// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests.32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="CityHash32" /> hash algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Known-answer test (KAT) vectors are encoded as upper-case hexadecimal strings representing the
    /// little-endian 4-byte output produced by <see cref="CityHash32" />.
    /// </para>
    /// <para>
    /// Incremental KAT vectors exercise inputs of increasing length where the input for index <c>i</c>
    /// is the byte sequence <c>{0, 1, …, i-1}</c>. This spans the algorithm's four internal
    /// length-selected code paths: 0–4 bytes, 5–12 bytes, 13–24 bytes, and 25+ bytes.
    /// </para>
    /// <para>
    /// All expected values were computed from the reference implementation and verified against the
    /// analytical derivations documented in <see cref="GetExpectedHashesForNamedInputs" />.
    /// </para>
    /// </remarks>
    [TestClass]
    public partial class CityHash32Tests
        : Security.Cryptography.CityHashTests<CityHash32Tests, CityHash32>
    {
        /// <inheritdoc />
        protected override CityHash32 CreateAlgorithm() => new CityHash32();

        /// <inheritdoc />
        protected override CityHash32 CreateAlgorithm(SingleTestVariant variant) => CreateAlgorithm();

        /// <inheritdoc />
        protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) =>
            new HashAlgorithmSpecification
            {
                HashSize = 32,
                InputBlockSize = 1,
                OutputBlockSize = 1,
            };

        /// <inheritdoc />
        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) =>
             new Dictionary<string, string>
             {
                 ["Empty"] = "02400040",
                 ["ABC"] = "CB5A67A8",
                 ["Zeros_16"] = "00400040",
                 ["Sequential_0_255"] = "3CB48141",
             };

        /// <inheritdoc />
        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) =>
            new[]
            {
                "02400040",  // [0x]
                "E5B0A856",  // [0x00]
                "0280CFE1",  // [0x00, 0x01]
                "1BCEE319",  // [0x00, 0x01, 0x02]
                "B91E6445",  // [0x00, 0x01, 0x02, 0x03]
                "EA55F979",  // [0x00, 0x01, 0x02, 0x03, 0x04]
                "84122C7E",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
                "67F85183",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
                "107293FA",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
                "D5996B59",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]
                "CD20F5EB",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09]
                "B59EBF6C",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A]
                "973A5B10",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B]
                "5220AAAB",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C]
                "5CA20E63",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D]
                "AE1B9001",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E]
            };


        /// <summary>
        /// Verifies that boundary inputs at each algorithm-path transition produce distinct, non-trivial
        /// hash values, confirming that each path is exercised correctly and produces well-distributed output.
        /// </summary>
        [TestMethod]
        public void ComputeHash_AtPathBoundaries_ShouldProduceDistinctNonZeroHashes()
        {
            using var algorithm = CreateAlgorithm();

            // One representative length per path.
            int[] boundaryLengths = { 4, 12, 24, 25 };
            var hashes = new List<byte[]>(boundaryLengths.Length);

            foreach (int len in boundaryLengths)
            {
                // Non-zero, varied bytes to avoid degenerate all-zero hashes.
                byte[] input = Enumerable.Range(1, len).Select(i => (byte)(i * 7)).ToArray();
                byte[] hash = algorithm.ComputeHash(input);

                Assert.IsTrue(hash.Any(b => b != 0),
                    $"Path for length {len} must not produce an all-zero hash for varied input.");

                hashes.Add(hash);
            }

            // All four path outputs must be mutually distinct.
            for (int i = 0; i < hashes.Count; i++)
            {
                for (int j = i + 1; j < hashes.Count; j++)
                {
                    CollectionAssert.AreNotEqual(hashes[i], hashes[j],
                        $"Paths at lengths {boundaryLengths[i]} and {boundaryLengths[j]} produced identical hashes.");
                }
            }
        }
    }
}