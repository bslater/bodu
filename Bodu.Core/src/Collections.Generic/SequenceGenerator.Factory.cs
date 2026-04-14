// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.Factory.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Generic;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Creates a sequence from a custom enumerator factory.
    /// </summary>
    /// <typeparam name="TResult">The type of elements in the generated sequence.</typeparam>
    /// <param name="enumeratorFactory">
    /// A delegate that produces a new <see cref="IEnumerator{TResult}"/> each time the sequence is iterated.
    /// </param>
    /// <returns>An enumerable sequence generated from the custom enumerator.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="enumeratorFactory"/> is <see langword="null"/>.</exception>
    /// <remarks>This method is useful when integrating external or imperative enumerators into a deferred LINQ-style pipeline.</remarks>
    public static IEnumerable<TResult> Factory<TResult>(Func<IEnumerator<TResult>> enumeratorFactory)
    {
        ThrowHelper.ThrowIfNull(enumeratorFactory);

        return new AnonymousEnumerable<TResult>(enumeratorFactory);
    }
}