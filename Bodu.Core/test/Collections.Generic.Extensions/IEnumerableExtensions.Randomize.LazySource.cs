// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Randomize.LazySource.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IEnumerableExtensionsTests_Randomize
{
    /// <summary>
    /// Verifies that <see cref="IEnumerableExtensions.Randomize{T}(IEnumerable{T}, RandomizationMode, IRandomGenerator?, int?)" /> with
    /// <see cref="RandomizationMode.BufferAll" /> handles a lazy iterator that does not implement <see cref="IReadOnlyCollection{T}" />,
    /// forcing the <c>AppendRange</c> fall-back branch in <c>RandomizeBuffered</c>.
    /// </summary>
    [TestMethod]
    public void Randomize_BufferAll_WhenSourceIsLazyIterator_ShouldReturnPermutationOfElements()
    {
        var expected = Enumerable.Range(1, 8).ToArray();

        int[] result = LazyYield(8).Randomize(RandomizationMode.BufferAll, CreateSeededRng()).ToArray();

        CollectionAssert.AreEquivalent(expected, result);
    }

    private static IEnumerable<int> LazyYield(int count)
    {
        for (int i = 1; i <= count; i++)
            yield return i;
    }
}
