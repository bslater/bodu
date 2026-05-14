// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollection.IEnumerable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollection<T> :
    System.Collections.Generic.IEnumerable<T>
{
    /// <summary>
    /// Returns an enumerator that iterates the collection's elements in head-to-tail logical order.
    /// </summary>
    /// <returns>An <see cref="Enumerator"/> for the collection.</returns>
    /// <remarks>
    /// The enumerator captures a structural-version token at creation. Any subsequent structural mutation
    /// — including <c>Clear</c>, <c>TrimExcess</c>, or any of the derived-type mutators — invalidates the
    /// enumerator. The next <see cref="Enumerator.MoveNext"/> or <see cref="Enumerator.Reset"/> call throws
    /// <see cref="System.InvalidOperationException"/>.
    /// </remarks>
    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
}
