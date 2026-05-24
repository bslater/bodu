// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Randomize.LazyShuffle.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IEnumerableExtensionsTests_Randomize
{

    /// <summary>
    /// Verifies that <see cref="IEnumerableExtensions.Randomize{T}(IEnumerable{T}, RandomizationMode, IRandomGenerator?, int?)" /> with
    /// <see cref="RandomizationMode.LazyShuffle" /> and a non-positive count yields an empty sequence (covers the early
    /// <c>yield break</c> branch in <c>LazyShuffle</c>).
    /// </summary>
    [TestMethod]
    public void Randomize_LazyShuffle_WhenCountIsZero_ShouldYieldEmptySequence()
    {
        var source = Enumerable.Range(1, 10).ToArray();

        var result = source.Randomize(RandomizationMode.LazyShuffle, CreateSeededRng(), count: 0).ToArray();

        Assert.IsEmpty(result);
    }

}
