// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.Factory.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Wraps a user-supplied enumerator factory in an <see cref="IEnumerable{TResult}" /> so it can participate in LINQ
    /// pipelines.
    /// </summary>
    /// <typeparam name="TResult">The element type produced by the supplied enumerator.</typeparam>
    /// <param name="enumeratorFactory">
    /// A delegate that returns a fresh <see cref="IEnumerator{T}" /> on each call. Must not be <see langword="null" />.
    /// The delegate is invoked once per <c>foreach</c> / <c>GetEnumerator</c> call, so it must produce an independent
    /// enumerator each time to allow re-enumeration.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{TResult}" /> that defers all work to <paramref name="enumeratorFactory" /> at
    /// iteration time.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="enumeratorFactory" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the preferred bridge from imperative or external enumerator implementations into a deferred,
    /// LINQ-friendly sequence. Reach for it when an existing component exposes a <c>GetEnumerator</c>-shaped method but
    /// does not implement <see cref="IEnumerable{T}" />, or when the iteration logic is simpler to express by hand than
    /// via the other <see cref="SequenceGenerator" /> overloads.
    /// </para>
    /// <para>
    /// Argument validation runs eagerly; iteration itself is fully deferred. The wrapper is finite or infinite
    /// according to the supplied enumerator and is re-enumerable only if <paramref name="enumeratorFactory" /> returns
    /// a new, independent enumerator on every call.
    /// </para>
    /// <para>
    /// Allocations are limited to a single wrapper instance plus whatever the supplied enumerator allocates per
    /// iteration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Adapt an external pull-style API into an IEnumerable<T> pipeline.
    /// IEnumerable<int> randomBytes = SequenceGenerator.Factory(() =>
    /// {
    ///     var rng = new Random(42);
    ///     return Enumerable.Range(0, 4).Select(_ => rng.Next(0, 256)).GetEnumerator();
    /// });
    ///
    /// foreach (int b in randomBytes)
    ///     Console.Write($"{b} "); // => 79 235 64 230 (a deterministic run with seed 42)
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<TResult> Factory<TResult>(Func<IEnumerator<TResult>> enumeratorFactory)
    {
        ThrowHelper.ThrowIfNull(enumeratorFactory);

        return new AnonymousEnumerable<TResult>(enumeratorFactory);
    }
}
