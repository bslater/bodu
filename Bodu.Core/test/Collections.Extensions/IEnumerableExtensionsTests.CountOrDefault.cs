// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensionsTests.CountOrDefault.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_CountOrDefault
{

    /// <summary>
    /// Verifies that <c>CountOrDefault</c> on a non-generic array (which implements <see cref="ICollection" />)
    /// returns the array length without enumerating.
    /// </summary>
    [TestMethod]
    public void CountOrDefault_WhenSourceIsArray_ShouldReturnArrayLength()
    {
        IEnumerable source = new int[] { 10, 20, 30 };

        Assert.AreEqual(3, source.CountOrDefault());
    }

    /// <summary>
    /// Verifies that <c>CountOrDefault</c> returns zero for an empty collection via the <see cref="ICollection" /> fast path.
    /// </summary>
    [TestMethod]
    public void CountOrDefault_WhenSourceIsEmptyCollection_ShouldReturnZero()
    {
        IEnumerable source = new ArrayList();

        Assert.AreEqual(0, source.CountOrDefault());
    }

    /// <summary>
    /// Verifies that <c>CountOrDefault</c> returns zero for an empty lazily-enumerated sequence via the fallback path.
    /// </summary>
    [TestMethod]
    public void CountOrDefault_WhenSourceIsEmptyEnumerable_ShouldReturnZero()
    {
        IEnumerable source = Empty();

        Assert.AreEqual(0, source.CountOrDefault());

        static IEnumerable Empty()
        {
            yield break;
        }
    }
    /// <summary>
    /// Verifies that <c>CountOrDefault</c> uses the <see cref="ICollection.Count" /> fast path
    /// when the source implements <see cref="ICollection" />.
    /// </summary>
    [TestMethod]
    public void CountOrDefault_WhenSourceIsIcollection_ShouldReturnCollectionCount()
    {
        IEnumerable source = new ArrayList { 1, 2, 3, 4, 5 };

        Assert.AreEqual(5, source.CountOrDefault());
    }

    /// <summary>
    /// Verifies that <c>CountOrDefault</c> falls back to full enumeration when the source implements neither
    /// <see cref="ICollection" /> nor <see cref="IReadOnlyCollection{T}" /> of <see cref="object" />.
    /// </summary>
    [TestMethod]
    public void CountOrDefault_WhenSourceIsPlainEnumerable_ShouldCountByEnumeration()
    {
        IEnumerable source = EnumerateThreeValues();

        Assert.AreEqual(3, source.CountOrDefault());

        static IEnumerable EnumerateThreeValues()
        {
            yield return 1;
            yield return 2;
            yield return 3;
        }
    }

    /// <summary>
    /// Verifies that <c>CountOrDefault</c> uses the <see cref="IReadOnlyCollection{T}.Count" /> fast path
    /// when the source implements <see cref="IReadOnlyCollection{T}" /> of <see cref="object" /> but not <see cref="ICollection" />.
    /// </summary>
    [TestMethod]
    public void CountOrDefault_WhenSourceIsReadOnlyCollectionOfObject_ShouldReturnReadOnlyCount()
    {
        IEnumerable source = new ReadOnlyCountOnly(7);

        Assert.AreEqual(7, source.CountOrDefault());
    }

    /// <summary>
    /// Provides a non-generic <see cref="IEnumerable" /> that also implements
    /// <see cref="IReadOnlyCollection{T}" /> of <see cref="object" /> but not <see cref="ICollection" />,
    /// used to exercise the second fast-path in <c>CountOrDefault</c>.
    /// </summary>
    private sealed class ReadOnlyCountOnly
        : IEnumerable
        , IReadOnlyCollection<object>
    {

        private readonly int _count;

        public ReadOnlyCountOnly(int count)
        {
            _count = count;
        }

        public int Count => _count;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<object> GetEnumerator()
        {
            for (var i = 0; i < _count; i++)
                yield return i;
        }

    }

}
