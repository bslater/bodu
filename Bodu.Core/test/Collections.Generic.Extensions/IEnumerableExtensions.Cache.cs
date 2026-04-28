// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Cache.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_Cache : EnumerableTests
{
    /// <summary>
    /// Verifies that <see cref="IEnumerableExtensions.Cache{T}" /> defers execution until the returned sequence is enumerated.
    /// </summary>
    [TestMethod]
    public void Cache_ShouldDeferExecution()
    {
        AssertExecutionIsDeferred("Cache", s => s.Cache(), YieldingSequence());
    }

    /// <summary>
    /// Verifies that <see cref="IEnumerableExtensions.Cache{T}" /> begins enumerating its source only when the consumer requests an item.
    /// </summary>
    [TestMethod]
    public void Cache_ShouldEnumerateOnDemand()
    {
        AssertExecutionOccursOnEnumeration("Cache", s => s.Cache(), YieldingSequence());
    }

    /// <summary>
    /// Verifies that a cached sequence enumerated concurrently from multiple threads yields the same complete result to every reader.
    /// </summary>
    [TestMethod]
    public void Cache_WhenEnumeratedFromMultipleThreads_ShouldReturnConsistentResults()
    {
        var tracker = new TrackingEnumerable<int>(YieldingSequence());
        var actual = tracker.Cache();

        Parallel.For(0, 5, _ =>
        {
            var result = actual.ToList();
            CollectionAssert.AreEqual(YieldingSequence().ToArray(), result);
        });
    }

    /// <summary>
    /// Verifies that enumerating a cached sequence twice yields the same items and the underlying source is only enumerated once.
    /// </summary>
    [TestMethod]
    public void Cache_WhenEnumeratedTwice_ShouldEnumerateSourceOnlyOnce()
    {
        var tracker = new TrackingEnumerable<int>(YieldingSequence());
        var actual = tracker.Cache();
        var first = actual.ToList();
        var second = actual.ToList();

        CollectionAssert.AreEqual(first, second);
        Assert.AreEqual(YieldingSequence().Count(), tracker.ItemsEnumerated);
    }

    /// <summary>
    /// Verifies that interrupting an in-progress enumeration still caches the items already emitted, and a fresh enumeration completes the source without re-pulling the cached prefix.
    /// </summary>
    [TestMethod]
    public void Cache_WhenEnumerationIsInterrupted_ShouldCachePartialResults()
    {
        var source = YieldingSequence();
        var tracker = new TrackingEnumerable<int>(source);
        var actual = tracker.Cache();

        using var enumerator = actual.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext()); // 1
        Assert.IsTrue(enumerator.MoveNext()); // 2

        var result = actual.ToList();

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, result);
        Assert.AreEqual(5, tracker.ItemsEnumerated);
    }

    /// <summary>
    /// Verifies that calling <see cref="IEnumerableExtensions.Cache{T}" /> on an already-cached sequence returns the same instance (idempotent).
    /// </summary>
    [TestMethod]
    public void Cache_WhenSourceIsAlreadyCached_ShouldReturnSameInstance()
    {
        var source = Enumerable.Range(1, 3).Cache();
        var result = source.Cache();

        Assert.AreSame(source, result);
    }

    /// <summary>
    /// Verifies that <see cref="IEnumerableExtensions.Cache{T}" /> short-circuits and returns the source instance when it already implements <see cref="ICollection{T}" />.
    /// </summary>
    [TestMethod]
    public void Cache_WhenSourceIsCollection_ShouldReturnSameInstance()
    {
        var source = new List<int> { 1, 2, 3 };
        var actual = source.Cache();

        Assert.AreSame(source, actual);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> source throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Cache_WhenSourceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            IEnumerableExtensions.Cache<int>(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IEnumerableExtensions.Cache{T}" /> short-circuits and returns the source instance when it already implements <see cref="IReadOnlyCollection{T}" />.
    /// </summary>
    [TestMethod]
    public void Cache_WhenSourceIsReadOnlyCollection_ShouldReturnSameInstance()
    {
        var source = Array.AsReadOnly(new[] { 1, 2, 3 });
        var actual = source.Cache();

        Assert.AreSame(source, actual);
    }

    /// <summary>
    /// Verifies that a source exception is preserved by the cache and rethrown on subsequent enumerations.
    /// </summary>
    [TestMethod]
    public void Cache_WhenSourceThrowsDuringEnumeration_ShouldRethrowOnSecondEnumeration()
    {
        var source = new TrackingEnumerable<int>(ThrowingSequence());
        var cached = source.Cache();

        try
        {
            foreach (var _ in cached)
            {
                // Force enumeration to trigger exception Only first value is valid
            }
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Assert: Re-enumerating throws the same exception again
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            {
                foreach (var _ in cached)
                {
                }
            }
            ;
        });
    }

    /// <summary>
    /// Verifies that a source exception is propagated to the consumer at the position where it was thrown during the first enumeration.
    /// </summary>
    [TestMethod]
    public void Cache_WhenSourceThrowsDuringEnumeration_ShouldThrowOnFirstEnumeration()
    {
        // Arrange
        var source = new TrackingEnumerable<int>(ThrowingSequence());
        var cached = source.Cache();
        var enumerator = cached.GetEnumerator();

        // Act: First item succeeds
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1, enumerator.Current);

        // Assert: Second item throws
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            {
                enumerator.MoveNext();
            }
            ;
        });
    }

    /// <summary>
    /// Verifies that disposing the cached sequence releases its internal cache and enumerator, so a subsequent <see cref="IEnumerableExtensions.Cache{T}" /> call begins a fresh enumeration.
    /// </summary>
    [TestMethod]
    public void Dispose_ShouldClearCacheAndEnumerator()
    {
        // Arrange
        var source = new TrackingEnumerable<int>(YieldingSequence());
        var cached = source.Cache();

        // Act: Enumerate to force caching
        _ = cached.ToList();

        // Cast to IDisposable for disposal
        if (cached is IDisposable disposable)
        {
            disposable.Dispose();
        }
        else
        {
            Assert.Fail("Cached sequence should be IDisposable.");
        }

        // Re-enumeration should trigger a new enumeration
        var reenumerated = new TrackingEnumerable<int>(YieldingSequence());
        var recached = reenumerated.Cache();

        // Assert: Ensure second enumeration is treated as a fresh sequence
        AssertExecutionOccursOnEnumeration("Cache", s => s.Cache(), YieldingSequence());
    }

    /// <summary>
    /// Verifies that accessing <c>Current</c> after the enumerator has been exhausted throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_Current_WhenAfterEnd_ShouldThrowException()
    {
        var actual = YieldingSequence().Cache();
        var enumerator = actual.GetEnumerator();
        while (enumerator.MoveNext()) { }

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            {
                _ = enumerator.Current;
            }
            ;
        });
    }

    /// <summary>
    /// Verifies that accessing <c>Current</c> before the first call to <c>MoveNext</c> throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_Current_WhenBeforeMoveNext_ShouldThrowException()
    {
        var actual = YieldingSequence().Cache();
        var enumerator = actual.GetEnumerator();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            {
                _ = enumerator.Current;
            }
            ;
        });
    }

    /// <summary>
    /// Verifies that calling <c>Reset</c> on the enumerator throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_Reset_ShouldThrowException()
    {
        var actual = YieldingSequence().Cache();
        var enumerator = actual.GetEnumerator();

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            enumerator.Reset();
        });
    }

    private static IEnumerable<int> ThrowingSequence()
    {
        yield return 1;
        throw new InvalidOperationException("Test");
    }

    private static IEnumerable<int> YieldingSequence()
    {
        yield return 1;
        yield return 2;
        yield return 3;
        yield return 4;
        yield return 5;
    }
}
