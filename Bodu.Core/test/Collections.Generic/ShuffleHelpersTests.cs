// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShuffleHelpersTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

[TestClass]
public partial class ShuffleHelpersTests
    : EnumerableTests
{
    public TestContext TestContext { get; set; }

    /// <summary>
    /// Asserts that a shuffle distribution stored in a tracker matrix approximates a uniform distribution, with no more than a
    /// specified number of statistical outliers exceeding a 3σ deviation.
    /// </summary>
    /// <param name="tracker">
    /// A two-dimensional array where <c>tracker[i, j]</c> represents how many times value <c>j</c> appeared at index <c>i</c>.
    /// </param>
    /// <param name="size">The number of elements in the shuffle (i.e., the width and height of the matrix).</param>
    /// <param name="maxOutliers">The maximum number of values allowed to deviate beyond ±3 standard deviations before the test fails.</param>
    /// <param name="label">A descriptive label used in logging to distinguish the test source (e.g., "Shuffle", "ShuffleAndYield").</param>
    /// <exception cref="AssertFailedException">Thrown if the number of statistical outliers exceeds <paramref name="maxOutliers" />.</exception>
    private void AssertStatisticalUniformity(
        int[,] tracker,
        int size,
        int maxOutliers = 2,
        string label = "Shuffle")
    {
        var outliers = 0;

        for (var col = 0; col < size; col++) // For each value from 0 to (size - 1)
        {
            var total = 0;

            // Sum how many times this value appeared across all index positions
            for (var row = 0; row < size; row++)
                total += tracker[row, col];

            var average = total / (double)size; // Expected appearances per index (uniformly distributed)
            var stdDev = Math.Sqrt(average * (1 - 1.0 / size)); // Binomial standard deviation

            for (var row = 0; row < size; row++) // For each possible position
            {
                var diff = Math.Abs(tracker[row, col] - average); // Deviation from expected value

                // Flag outliers that deviate more than ±3σ from the mean
                if (diff > 3 * stdDev)
                {
                    outliers++;

                    // Log the outlier for diagnostic purposes
                    TestContext.WriteLine(
                        $"[{label}] OUTLIER: Value {col} in index {row}: {tracker[row, col]} " +
                        $"(diff={diff:F2}, limit={3 * stdDev:F2})");
                }
            }
        }

        // Fail the test if the number of outliers exceeds the permitted threshold
        Assert.IsTrue(outliers <= maxOutliers,
            $"[{label}] Too many statistical outliers: {outliers} exceeded ±3σ (allowed: {maxOutliers}).");
    }
}
