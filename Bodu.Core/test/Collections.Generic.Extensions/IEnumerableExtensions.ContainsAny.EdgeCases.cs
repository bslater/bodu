// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.ContainsAny.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IEnumerableExtensionsTests_ContainsAny
{
    /// <summary>
    /// Verifies the empty-itemSet guard branch: when <paramref name="items" /> implements legacy <see cref="ICollection" /> but not
    /// generic <see cref="ICollection{T}" />, <c>TryGetNonEnumeratedCount</c> succeeds so <c>EnsureMaterialized</c> returns the sequence
    /// as-is, the generic <c>itemsCount</c> probe yields <see langword="null" />, the <c>== 0</c> fast path is bypassed, and the empty
    /// <see cref="HashSet{T}" /> guard inside the else branch short-circuits to <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void ContainsAny_WhenItemsIsLegacyCollectionEmptyButNotGenericCollection_ShouldReturnFalse()
    {
        int[] source = [1, 2, 3, 4, 5];
        IEnumerable<int> items = new LegacyEmptyCollection<int>();

        Assert.IsFalse(source.ContainsAny(items));
    }

    /// <summary>
    /// Verifies that the same empty-itemSet guard branch returns <see langword="false" /> when an explicit equality comparer is supplied.
    /// </summary>
    [TestMethod]
    public void ContainsAny_WhenItemsIsLegacyCollectionEmptyWithComparer_ShouldReturnFalse()
    {
        string[] source = ["a", "b", "c"];
        IEnumerable<string> items = new LegacyEmptyCollection<string>();

        Assert.IsFalse(source.ContainsAny(items, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// An empty sequence that implements the legacy non-generic <see cref="ICollection" /> contract (so
    /// <see cref="System.Linq.Enumerable.TryGetNonEnumeratedCount{TSource}" /> succeeds) but deliberately does NOT implement
    /// <see cref="ICollection{T}" />. This forces <c>EnsureMaterialized</c> to return the original instance without converting it to an
    /// array, leaving the <c>items is ICollection&lt;T&gt;</c> probe as <see langword="false" /> and producing a <see langword="null" />
    /// <c>itemsCount</c> in <c>ContainsAny</c>.
    /// </summary>
    private sealed class LegacyEmptyCollection<T>
        : IEnumerable<T>
        , ICollection
    {
        public int Count => 0;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public void CopyTo(Array array, int index)
        {
            // Empty - nothing to copy.
        }

        public IEnumerator<T> GetEnumerator() => System.Linq.Enumerable.Empty<T>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
