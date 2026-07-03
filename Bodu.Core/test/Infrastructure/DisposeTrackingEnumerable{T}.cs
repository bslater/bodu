// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DisposeTrackingEnumerable{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Infrastructure;

/// <summary>
/// A test enumerable that wraps a backing sequence and records how many times it was enumerated, advanced, and
/// disposed, so iterator operators can be asserted to be single-pass and to dispose their enumerator.
/// </summary>
/// <typeparam name="T">The type of elements in the sequence.</typeparam>
/// <remarks>
/// Unlike <see cref="TrackingEnumerable{T}" />, which enforces single enumeration and counts item access, this helper
/// exposes explicit <see cref="GetEnumeratorCallCount" />, <see cref="MoveNextCallCount" />, and
/// <see cref="DisposeCallCount" /> counters so a test can assert an operator disposed the enumerator exactly once.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Assert that an operator enumerates its source once and disposes the enumerator.
/// var source = new DisposeTrackingEnumerable<int>(new[] { 1, 2, 3 });
/// _ = source.Pairwise().ToArray();
///
/// Assert.AreEqual(1, source.GetEnumeratorCallCount);
/// Assert.AreEqual(1, source.DisposeCallCount);
///]]>
/// </code>
/// </example>
public sealed class DisposeTrackingEnumerable<T>
    : IEnumerable<T>
{
    private readonly IEnumerable<T> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposeTrackingEnumerable{T}" /> class over the specified backing
    /// sequence.
    /// </summary>
    /// <param name="items">The backing sequence whose enumeration is tracked.</param>
    public DisposeTrackingEnumerable(IEnumerable<T> items) => _items = items;

    /// <summary>
    /// Gets the number of times <see cref="GetEnumerator" /> has been invoked.
    /// </summary>
    /// <value>The count of enumerator acquisitions.</value>
    public int GetEnumeratorCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="IEnumerator.MoveNext" /> has been invoked across all enumerators.
    /// </summary>
    /// <value>The count of advance operations.</value>
    public int MoveNextCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="IDisposable.Dispose" /> has been invoked across all enumerators.
    /// </summary>
    /// <value>The count of disposals.</value>
    public int DisposeCallCount { get; private set; }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        GetEnumeratorCallCount++;
        return new DisposeTrackingEnumerator(this, _items.GetEnumerator());
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// An enumerator that forwards to an inner enumerator while recording advance and disposal counts on the owning
    /// <see cref="DisposeTrackingEnumerable{T}" />.
    /// </summary>
    private sealed class DisposeTrackingEnumerator
        : IEnumerator<T>
    {
        private readonly DisposeTrackingEnumerable<T> _owner;
        private readonly IEnumerator<T> _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposeTrackingEnumerator" /> class.
        /// </summary>
        /// <param name="owner">The enumerable that owns the tracking counters.</param>
        /// <param name="inner">The wrapped enumerator to forward to.</param>
        public DisposeTrackingEnumerator(DisposeTrackingEnumerable<T> owner, IEnumerator<T> inner)
        {
            _owner = owner;
            _inner = inner;
        }

        /// <inheritdoc />
        public T Current => _inner.Current;

        /// <inheritdoc />
        object? IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            _owner.MoveNextCallCount++;
            return _inner.MoveNext();
        }

        /// <inheritdoc />
        public void Reset() => _inner.Reset();

        /// <inheritdoc />
        public void Dispose()
        {
            _owner.DisposeCallCount++;
            _inner.Dispose();
        }
    }
}
