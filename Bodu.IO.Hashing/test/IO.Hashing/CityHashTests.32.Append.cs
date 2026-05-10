// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests.32.Append.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class CityHash32Tests
{
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
