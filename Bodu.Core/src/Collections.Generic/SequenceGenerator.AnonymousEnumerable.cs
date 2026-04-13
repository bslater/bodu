// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.AnonymousEnumerable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bodu.Collections.Generic;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Represents a lazily evaluated enumerable sequence using a user-provided enumerator factory.
    /// </summary>
    /// <typeparam name="TResult">The type of elements returned by the enumerator.</typeparam>
    [DebuggerDisplay("AnonymousEnumerable<{typeof(TResult).Name}>")]
    private sealed class AnonymousEnumerable<TResult> : IEnumerable<TResult>
    {
        private readonly Func<IEnumerator<TResult>> _createEnumerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymousEnumerable{TResult}" /> class.
        /// </summary>
        /// <param name="createEnumerator">The delegate used to generate the enumerator.</param>
        internal AnonymousEnumerable(Func<IEnumerator<TResult>> createEnumerator)
        {
            _createEnumerator = createEnumerator ?? throw new ArgumentNullException(nameof(createEnumerator));
        }

        /// <inheritdoc />
        public IEnumerator<TResult> GetEnumerator() => _createEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
