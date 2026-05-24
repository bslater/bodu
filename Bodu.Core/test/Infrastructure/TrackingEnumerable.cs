// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TrackingEnumerable.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Infrastructure;

/// <summary>
/// A test utility that wraps an <see cref="IEnumerable{T}" /> and tracks enumeration behaviour.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class TrackingEnumerable<T>
    : IEnumerable<T>
{

    private readonly Action _onEnumerate;
    private readonly Action<int>? _onItemAccess;
    private readonly IEnumerable<T> _source;
    private bool _enforceSingleEnumeration;
    private bool _enumeratorCreated = false;

    private int _itemsEnumerated = 0;

    /// <summary>
    /// Creates a new <see cref="TrackingEnumerable{T}" /> instance.
    /// </summary>
    /// <param name="source">The underlying source sequence.</param>
    /// <param name="onEnumerate">An optional callback invoked when enumeration begins.</param>
    /// <param name="onItemAccess">An optional callback for each item accessed.</param>
    public TrackingEnumerable(IEnumerable<T> source, Action? onEnumerate = null, Action<int>? onItemAccess = null)
    {
        this._source = source ?? throw new ArgumentNullException(nameof(source));
        this._onEnumerate = onEnumerate ?? (() => { });
        this._onItemAccess = onItemAccess;
    }

    /// <summary>
    /// Gets whether the enumerable has been enumerated at least once.
    /// </summary>
    public bool HasEnumerated => _enumeratorCreated;

    /// <summary>
    /// Gets the number of items enumerated (only tracked in first enumeration).
    /// </summary>
    public int ItemsEnumerated => _itemsEnumerated;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Enables enforcement of one-time enumeration.
    /// </summary>
    public TrackingEnumerable<T> EnforceSingleEnumeration()
    {
        _enforceSingleEnumeration = true;
        return this;
    }

    public IEnumerator<T> GetEnumerator()
    {
        if (_enumeratorCreated && _enforceSingleEnumeration)
            throw new InvalidOperationException("Sequence cannot be enumerated more than once.");

        _enumeratorCreated = true;
        _onEnumerate();

        var index = 0;
        foreach (T? item in _source)
        {
            _onItemAccess?.Invoke(index);
            _itemsEnumerated = ++index;
            yield return item;
        }
    }

}
