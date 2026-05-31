// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.ContainsAny.NoMatch.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IEnumerableExtensionsTests_ContainsAny
{

    /// <summary>
    /// Verifies that <c>ContainsAny</c> returns <see langword="false" /> via the build-from-items branch when there is
    /// no overlap and source is a non-collection lazy iterator.
    /// </summary>
    [TestMethod]
    public void ContainsAny_WhenSourceIsLazyAndItemsHaveNoOverlap_ShouldReturnFalse()
    {
        int[] items = [1, 2, 3];
        IEnumerable<int> source = LazyYield(100, 5);            // 100..104, disjoint from items

        Assert.IsFalse(source.ContainsAny(items));

        static IEnumerable<int> LazyYield(int start, int count)
        {
            for (var i = 0; i < count; i++)
                yield return start + i;
        }
    }
    /// <summary>
    /// Verifies that <c>ContainsAny</c> returns <see langword="false" /> via the source-is-smaller branch when there is
    /// no overlap between source and items.
    /// </summary>
    [TestMethod]
    public void ContainsAny_WhenSourceIsSmallerAndNoOverlap_ShouldReturnFalse()
    {
        int[] source = [-1, -2];                              // small ICollection<int>
        var items = Enumerable.Range(0, 50).ToArray();        // larger ICollection<int> with no overlap

        Assert.IsFalse(source.ContainsAny(items));
    }

}
