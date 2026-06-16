// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Cache.CtorAndDispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IEnumerableExtensionsTests_Cache
{

    /// <summary>
    /// Verifies that the private <c>CacheEnumerable&lt;T&gt;</c> constructor throws
    /// <see cref="ArgumentNullException" /> with the expected parameter name when the source argument is
    /// <see langword="null" />, exercising the constructor's null guard directly (independently of the public
    /// <see cref="IEnumerableExtensions.Cache{T}" /> overload's switch-expression null branch).
    /// </summary>
    [TestMethod]
    public void CacheEnumerableCtor_WhenSourceIsNull_ShouldThrowExactly()
    {
        Type cacheEnumerableType = GetCacheEnumerableType<int>();
        ConstructorInfo ctor = cacheEnumerableType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(IEnumerable<int>)],
            modifiers: null)
            ?? throw new InvalidOperationException("CacheEnumerable<T> constructor not found.");

        TargetInvocationException ex = Assert.ThrowsExactly<TargetInvocationException>(() =>
        {
            _ = ctor.Invoke([null]);
        });

        Assert.IsInstanceOfType<ArgumentNullException>(ex.InnerException);
        Assert.AreEqual("source", ((ArgumentNullException)ex.InnerException!).ParamName);
    }

    /// <summary>
    /// Verifies that disposing a cached sequence after it has been fully materialised resets the wrapper's internal
    /// initialisation state, so that a fresh <see cref="IEnumerable{T}.GetEnumerator" /> call after the dispose
    /// triggers a brand-new source enumeration rather than returning the previously cached prefix.
    /// </summary>
    [TestMethod]
    public void Dispose_AfterFullEnumeration_ShouldReinitialiseFromSourceOnNextEnumeration()
    {
        int sourceEnumerations = 0;
        var tracker = new TrackingEnumerable<int>(YieldingSequence(), onEnumerate: () => sourceEnumerations++);
        IEnumerable<int> cached = tracker.Cache();
        Assert.IsInstanceOfType<IDisposable>(cached);

        // First pass materialises the cache and triggers exactly one source enumeration.
        var firstPass = cached.ToList();
        Assert.AreEqual(1, sourceEnumerations,
            "First enumeration of a freshly cached sequence must enumerate the source once.");

        ((IDisposable)cached).Dispose();

        // After dispose, the wrapper resets its initialisation state to 0 and clears _cache.
        // A subsequent enumeration must re-enter the EnsureInitialized claim path and pull
        // values from the source again, so the on-enumerate callback fires a second time.
        var secondPass = cached.ToList();

        CollectionAssert.AreEqual(firstPass, secondPass);
        Assert.AreEqual(2, sourceEnumerations,
            "Dispose must clear cached state so subsequent enumeration re-walks the source.");
    }

    /// <summary>
    /// Verifies that disposing a cached sequence before any element has been requested releases the wrapper's state
    /// without throwing — the constructor-only state must be safe to release even when initialisation has not begun.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledBeforeEnumeration_ShouldNotThrow()
    {
        IEnumerable<int> cached = new TrackingEnumerable<int>(YieldingSequence()).Cache();
        Assert.IsInstanceOfType<IDisposable>(cached, "Lazy cached sequence must be disposable.");

        ((IDisposable)cached).Dispose();
    }

    /// <summary>
    /// Verifies that disposing the cached sequence while an active enumerator is still in progress causes the next
    /// <c>MoveNext</c> on that enumerator to throw <see cref="ObjectDisposedException" />, confirming that the dispose
    /// path zeroes the cache field that <c>Enumerator.MoveNext</c> dereferences on every call.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledMidEnumeration_ShouldCauseLiveEnumeratorMoveNextToThrowObjectDisposed()
    {
        IEnumerable<int> cached = new TrackingEnumerable<int>(YieldingSequence()).Cache();
        Assert.IsInstanceOfType<IDisposable>(cached);

        IEnumerator<int> active = cached.GetEnumerator();
        Assert.IsTrue(active.MoveNext());
        Assert.IsTrue(active.MoveNext());

        ((IDisposable)cached).Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            active.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="IDisposable.Dispose" /> twice on a cached sequence is idempotent and never
    /// throws.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledMultipleTimes_ShouldBeIdempotent()
    {
        var tracker = new TrackingEnumerable<int>(YieldingSequence());
        IEnumerable<int> cached = tracker.Cache();
        Assert.IsInstanceOfType<IDisposable>(cached);

        _ = cached.ToList();

        var disposable = (IDisposable)cached;
        disposable.Dispose();
        disposable.Dispose();
    }

    /// <summary>
    /// Resolves the closed generic <c>CacheEnumerable&lt;T&gt;</c> nested type defined by
    /// <see cref="IEnumerableExtensions" /> so test code can construct it directly without going through the public
    /// factory.
    /// </summary>
    /// <typeparam name="T">The element type for the closed generic <c>CacheEnumerable&lt;T&gt;</c>.</typeparam>
    /// <returns>The closed generic <see cref="Type" /> for <c>CacheEnumerable&lt;T&gt;</c>.</returns>
    private static Type GetCacheEnumerableType<T>()
    {
        Type openGeneric = typeof(IEnumerableExtensions)
            .GetNestedType("CacheEnumerable`1", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CacheEnumerable<T> nested type not found.");
        return openGeneric.MakeGenericType(typeof(T));
    }

}
