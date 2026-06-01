// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides static factory methods that produce lazily evaluated <see cref="IEnumerable{T}" /> sequences without
/// materializing the underlying collection.
/// </summary>
/// <remarks>
/// <para>
/// The factory surface mirrors the conventions of <see cref="System.Linq.Enumerable" /> and complements it with
/// sequence shapes that LINQ does not provide directly. The catalogue currently covers numeric and value projections (
/// <c>Range</c>, <c>Repeat</c>), stateful generation (<c>NextWhile</c>), and arbitrary enumerator adaptation (
/// <c>Factory</c>). Every member returns a deferred sequence that produces elements only as the consumer iterates;
/// nothing is allocated up front for the result set itself.
/// </para>
/// <para>
/// <c>Range</c> overloads accept either an inclusive start / exclusive stop pair (with an inferred step direction) or
/// an explicit step, and operate over <see cref="int" /> and <see cref="long" /> domains. <c>Repeat</c> produces either
/// a finite or unbounded sequence of a single value, supporting both reference and value types. <c>NextWhile</c> drives
/// a state-machine-style generator from an initial state and a transition delegate, terminating when the supplied
/// predicate is no longer satisfied. <c>Factory</c> wraps a delegate-returned <see cref="IEnumerator{T}" /> so callers
/// can adapt non-collection iteration sources to the LINQ pipeline.
/// </para>
/// <para>
/// All sequences returned by this type are single-pass with respect to side effects in their generator delegate:
/// re-enumerating the returned <see cref="IEnumerable{T}" /> invokes the supplied delegates again. Callers that require
/// a stable, replayable view should materialize the sequence via <c>ToArray</c> or <c>ToList</c>.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// A counted descending range.
/// foreach (int n in SequenceGenerator.Range(start: 10, stop: 0, step: -2))
///     Console.WriteLine(n); // 10, 8, 6, 4, 2
///
/// A finite repeat.
/// string[] padding = SequenceGenerator.Repeat("--", count: 3).ToArray();
///
/// A stateful generator producing powers of two while the value fits in a positive int.
/// IEnumerable<int> powers = SequenceGenerator.NextWhile(
///     initialValue: 1,
///     conditionHandler: value => value > 0,
///     resultSelector: prev => prev * 2);
///]]>
/// </example>
public static partial class SequenceGenerator
{
}
