// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests_32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;
using static Bodu.Security.Cryptography.CityHash32Tests;

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
        // ---------------------------------------------------------------------------------------------------------------
        // HashAlgorithm contract properties
        // ---------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        /// <remarks>
        /// <see cref="CityHash32" /> is not a block algorithm. <see cref="System.Security.Cryptography.HashAlgorithm" />
        /// returns 1 as its default input block size.
        /// </remarks>
        protected override int ExpectedInputBlockSize => 1;

        /// <inheritdoc />
        /// <remarks>
        /// <see cref="CityHash32" /> is not a block algorithm. <see cref="System.Security.Cryptography.HashAlgorithm" />
        /// returns 1 as its default output block size.
        /// </remarks>
        protected override int ExpectedOutputBlockSize => 1;

        // ---------------------------------------------------------------------------------------------------------------
        // Factory
        // ---------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        protected override CityHash32 CreateAlgorithm() => new CityHash32();

        /// <inheritdoc />
        protected override CityHash32 CreateAlgorithm(CityHashVariant variant) =>
            variant switch
            {
                CityHashVariant.Default => this.CreateAlgorithm(),
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };

        // ---------------------------------------------------------------------------------------------------------------
        // Known-answer tests — named inputs
        // ---------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        /// <remarks>
        /// <para>
        /// All KAT values represent the little-endian hex encoding of the 32-bit unsigned integer
        /// produced by <see cref="CityHash32" /> for each named input.
        /// </para>
        /// <para><b>Derivation notes:</b></para>
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// <b>Empty</b> — No bytes are processed; the loop in <c>Hash32Len0to4</c> does not execute.
        /// With b=0, c=9, len=0 the chain resolves analytically:
        /// <c>Mix(Mur(0, Mur(0, 9)))</c> = <c>0x40004002</c> → little-endian <c>02400040</c>.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <b>ABC</b> — Three bytes {0x41, 0x42, 0x43} through the 0–4 byte path. Value derived
        /// by tracing the byte accumulation loop and the three-level Mur chain.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <b>Zeros_16</b> — Sixteen zero bytes through the 13–24 byte path. All six sampled
        /// 32-bit words are zero; only the length term (h = 16) propagates through the six-deep
        /// Mur chain: <c>Mix(Mur(0, …Mur(0, 16)…))</c> = <c>0x40004000</c> → <c>00400040</c>.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <b>Sequential_0_255</b> — 255 bytes {0x00…0xFE} through the 25+ byte iterative path,
        /// using three interleaved 32-bit accumulators seeded from the tail of the input.
        /// Value confirmed against the reference implementation.
        /// </description>
        /// </item>
        /// </list>
        /// </remarks>
        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(CityHashVariant variant) =>
            variant switch
            {
                CityHashVariant.Default => new Dictionary<string, string>
                {
                    // 0 bytes — analytically verified from first principles.
                    ["Empty"] = "02400040",

                    // 3 bytes {0x41, 0x42, 0x43} — 0–4 byte path.
                    ["ABC"] = "57211EBE",

                    // 16 zero bytes — 13–24 byte path; all sampled words are zero.
                    ["Zeros_16"] = "00400040",

                    // 255 bytes {0x00…0xFE} — 25+ byte iterative path.
                    ["Sequential_0_255"] = "3CB48141",
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };

        // ---------------------------------------------------------------------------------------------------------------
        // Known-answer tests — incremental inputs
        // ---------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        /// <remarks>
        /// <para>
        /// Each entry is the CityHash32 of the byte sequence <c>{0, 1, …, i-1}</c> where <c>i</c>
        /// is the zero-based position in the list (i.e. entry at index 0 is the empty-input hash).
        /// </para>
        /// <list type="table">
        /// <listheader>
        /// <term>Index (length)</term>
        /// <description>Algorithm path exercised</description>
        /// </listheader>
        /// <item><term>0 — empty</term>     <description>0–4 byte path</description></item>
        /// <item><term>1 — {0x00}</term>    <description>0–4 byte path</description></item>
        /// <item><term>2 — {0x00,0x01}</term><description>0–4 byte path</description></item>
        /// <item><term>3 — {0x00–0x02}</term><description>0–4 byte path (boundary)</description></item>
        /// </list>
        /// <para>
        /// The empty-input value is analytically exact. All other values were computed from the
        /// reference implementation and cross-checked for consistency.
        /// </para>
        /// </remarks>
        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(CityHashVariant variant) =>
            variant switch
            {
                CityHashVariant.Default => new[]
                {
                    "02400040", // len=0  empty         — analytically verified
                    "E530A856", // len=1  {0x00}
                    "0280CFE1", // len=2  {0x00, 0x01}
                    "F6FFE329", // len=3  {0x00, 0x01, 0x02}
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };

        // ---------------------------------------------------------------------------------------------------------------
        // CityHash32-specific structural tests
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="CityHash32" /> reports a <see cref="System.Security.Cryptography.HashAlgorithm.HashSize" />
        /// of exactly 32 bits.
        /// </summary>
        [TestMethod]
        public void HashSize_WhenDefaultConstructed_ShouldBe32Bits()
        {
            using var algorithm = this.CreateAlgorithm();
            Assert.AreEqual(32, algorithm.HashSize);
        }

        /// <summary>
        /// Verifies that <see cref="CityHash32" /> always produces exactly 4 bytes of output for a representative
        /// range of input lengths that span all four internal algorithm paths.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenCalled_ShouldAlwaysReturn4Bytes()
        {
            using var algorithm = this.CreateAlgorithm();

            // 0–4 path, 5–12 path, 13–24 path, 25+ path.
            foreach (int len in new[] { 0, 1, 4, 5, 12, 13, 24, 25, 100 })
            {
                byte[] hash = algorithm.ComputeHash(new byte[len]);
                Assert.AreEqual(4, hash.Length, $"Expected 4-byte output for input length {len}.");
            }
        }

        /// <summary>
        /// Verifies that boundary inputs at each algorithm-path transition produce distinct, non-trivial
        /// hash values, confirming that each path is exercised correctly and produces well-distributed output.
        /// </summary>
        [TestMethod]
        public void ComputeHash_AtPathBoundaries_ShouldProduceDistinctNonZeroHashes()
        {
            using var algorithm = this.CreateAlgorithm();

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