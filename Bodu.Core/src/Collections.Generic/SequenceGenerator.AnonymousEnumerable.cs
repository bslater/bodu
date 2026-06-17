// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.AnonymousEnumerable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics;

namespace Bodu.Collections.Generic;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Represents a lazily evaluated enumerable sequence using a user-provided enumerator factory.
    /// </summary>
    /// <typeparam name="TResult">The type of elements returned by the enumerator.</typeparam>
    [DebuggerDisplay("AnonymousEnumerable<{typeof(TResult).Name}>")]
    private sealed class AnonymousEnumerable<TResult>
        : IEnumerable<TResult>
    {
        /// <summary>
        /// The factory delegate invoked to produce a fresh enumerator for each enumeration.
        /// </summary>
        private readonly Func<IEnumerator<TResult>> _createEnumerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymousEnumerable{TResult}" /> class.
        /// </summary>
        /// <param name="createEnumerator">The delegate used to generate the enumerator.</param>
        internal AnonymousEnumerable(Func<IEnumerator<TResult>> createEnumerator)
        {
            _createEnumerator = createEnumerator;
        }

        /// <inheritdoc />
        public IEnumerator<TResult> GetEnumerator() => _createEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
