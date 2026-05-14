// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Cache.InitializationFailure.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IEnumerableExtensionsTests_Cache
{
    /// <summary>
    /// Verifies that the cached sequence captures and re-throws an exception thrown by the source's
    /// <see cref="IEnumerable{T}.GetEnumerator" /> on first enumeration, then re-throws the same captured exception on subsequent
    /// enumerations rather than re-invoking the failing source.
    /// </summary>
    [TestMethod]
    public void Cache_WhenSourceGetEnumeratorThrows_ShouldCaptureAndRethrowOnSubsequentEnumerations()
    {
        var source = new ThrowingSource();
        IEnumerable<int> cached = source.Cache();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            using IEnumerator<int> first = cached.GetEnumerator();
            first.MoveNext();
        });

        Assert.AreEqual(1, source.GetEnumeratorCallCount, "Source GetEnumerator should be invoked exactly once.");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            using IEnumerator<int> second = cached.GetEnumerator();
            second.MoveNext();
        });

        Assert.AreEqual(1, source.GetEnumeratorCallCount, "Source GetEnumerator should not be invoked again on subsequent enumerations.");
    }

    /// <summary>
    /// A source whose <see cref="IEnumerable{T}.GetEnumerator" /> always throws <see cref="InvalidOperationException" />, counting the
    /// number of invocations so the test can assert that the failure is captured and re-used by the cache.
    /// </summary>
    private sealed class ThrowingSource
        : IEnumerable<int>
    {
        public int GetEnumeratorCallCount { get; private set; }

        public IEnumerator<int> GetEnumerator()
        {
            GetEnumeratorCallCount++;
            throw new InvalidOperationException("source enumerator factory failed");
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
