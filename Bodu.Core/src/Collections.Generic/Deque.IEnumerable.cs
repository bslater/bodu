// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Deque.IEnumerable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;

namespace Bodu.Collections.Generic;

public partial class Deque<T> :
    System.Collections.Generic.IEnumerable<T>
{
    /// <summary>
    /// Returns an enumerator that iterates over the deque's contents in head-to-tail order.
    /// </summary>
    /// <returns>An <see cref="Enumerator"/> that provides forward-only, read-only access to the elements.</returns>
    /// <remarks>
    /// The enumerator captures a structural-version token at creation. Any structural modification — including
    /// <see cref="AddFirst"/>, <see cref="AddLast"/>, <see cref="RemoveFirst"/>, <see cref="RemoveLast"/>,
    /// <see cref="Clear"/>, <see cref="EnsureCapacity"/>, or <see cref="TrimExcess"/> — invalidates the
    /// enumerator. Subsequent <see cref="Enumerator.MoveNext"/> or <see cref="Enumerator.Reset"/> calls throw
    /// <see cref="System.InvalidOperationException"/>.
    /// </remarks>
    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
}
