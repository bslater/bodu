// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests.32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="CityHash32" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class CityHash32Tests
    : CityHashTests<CityHash32Tests, CityHash32>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 4,
        KnownAnswers = new()
        {
            Empty = "02400040",
            Abc = "CB5A67A8",
            Zeros16 = "00400040",
            Sequential0To255 = "3CB48141",
        },
    };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented CityHash32 known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IEnumerable<string> GetIncrementalHashValue(SingleTestVariant variant) => new[]
    {
        "02400040", "E5B0A856", "0280CFE1", "1BCEE319", "B91E6445", "EA55F979",
        "84122C7E", "67F85183", "107293FA", "D5996B59", "CD20F5EB", "B59EBF6C",
        "973A5B10", "5220AAAB", "5CA20E63", "AE1B9001",
    };

    /// <summary>
    /// Verifies that boundary inputs at each algorithm-path transition produce distinct, non-trivial hash
    /// values, confirming that each path is exercised correctly and produces well-distributed output.
    /// </summary>
    [TestMethod]
    public void Append_AtPathBoundaries_ShouldProduceDistinctNonZeroHashes()
    {
        int[] boundaryLengths = { 4, 12, 24, 25 };
        var hashes = new List<byte[]>(boundaryLengths.Length);

        foreach (int len in boundaryLengths)
        {
            byte[] input = Enumerable.Range(1, len).Select(i => (byte)(i * 7)).ToArray();
            CityHash32 algorithm = CreateAlgorithm();
            algorithm.Append(input);
            byte[] hash = algorithm.GetCurrentHash();

            Assert.IsTrue(hash.Any(b => b != 0),
                $"Path for length {len} must not produce an all-zero hash for varied input.");

            hashes.Add(hash);
        }

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
