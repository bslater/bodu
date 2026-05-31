// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.ContainsAll.LazyItems.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IEnumerableExtensionsTests_ContainsAll
{

    /// <summary>
    /// Verifies that <c>ContainsAll</c> returns the correct result when <paramref name="items" /> is a lazy iterator
    /// (not an <see cref="ICollection{T}" />), exercising the branch where the empty-collection fast path is skipped.
    /// </summary>
    [TestMethod]
    public void ContainsAll_WhenItemsIsLazyIterator_ShouldReturnExpectedResult()
    {
        int[] source = [1, 2, 3, 4, 5];

        Assert.IsTrue(source.ContainsAll(LazyYield(2, 3))); // 2,3,4 all present
        Assert.IsFalse(source.ContainsAll(LazyYield(4, 4))); // 4,5,6,7 -> 6 missing

        static IEnumerable<int> LazyYield(int start, int count)
        {
            for (var i = 0; i < count; i++)
                yield return start + i;
        }
    }

}
