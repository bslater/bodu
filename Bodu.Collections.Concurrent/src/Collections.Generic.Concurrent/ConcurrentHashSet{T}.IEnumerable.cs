// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSet{T}.IEnumerable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentHashSet<T> :
    IEnumerable<T>
{
    /// <summary>
    /// Returns an enumerator that iterates over a weakly consistent snapshot of the set.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> over the elements present when the enumerator was created.</returns>
    /// <remarks>
    /// <para>
    /// Enumeration operates on a weakly consistent snapshot captured via <see cref="ToArray" /> at the moment this
    /// method is called. Elements added or removed after the snapshot completes are not reflected in the enumerated
    /// sequence; elements mutated while the snapshot is being captured may or may not appear.
    /// </para>
    /// <para>
    /// Because the enumerator operates on a snapshot, it never throws <see cref="System.InvalidOperationException" />
    /// due to concurrent modification — unlike enumerators on non-concurrent collections.
    /// </para>
    /// </remarks>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <inheritdoc cref="GetEnumerator" />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc cref="GetEnumerator" />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();
}
