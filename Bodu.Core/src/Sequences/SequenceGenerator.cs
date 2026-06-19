// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Sequences;

/// <summary>
/// Provides static factory methods that produce lazily evaluated <see cref="IEnumerable{T}" /> sequences without
/// materializing the underlying collection — both general-purpose shapes (<c>Range</c>, <c>Repeat</c>, <c>NextWhile</c>,
/// <c>Factory</c>) and a catalogue of well-known mathematical sequences (Fibonacci, Farey, Leibniz, look-and-say, and
/// Thue–Morse).
/// </summary>
/// <remarks>
/// <para>
/// The factory surface mirrors the conventions of <see cref="System.Linq.Enumerable" /> and complements it with
/// sequence shapes that LINQ does not provide directly. Every member returns a deferred sequence that produces elements
/// only as the consumer iterates; nothing is allocated up front for the result set itself.
/// </para>
/// <para>
/// The general-purpose primitives cover numeric and value projections, stateful generation, and enumerator adaptation.
/// <c>Range</c> overloads accept either an inclusive start / exclusive stop pair (with an inferred step direction) or
/// an explicit step, and operate over <see cref="int" /> and <see cref="long" /> domains. <c>Repeat</c> produces either
/// a finite or unbounded sequence of a single value, supporting both reference and value types. <c>NextWhile</c> drives
/// a state-machine-style generator from an initial state and a transition delegate, terminating when the supplied
/// predicate is no longer satisfied. <c>Factory</c> wraps a delegate-returned <see cref="IEnumerator{T}" /> so callers
/// can adapt non-collection iteration sources to the LINQ pipeline.
/// </para>
/// <para>
/// The mathematical catalogue is intentionally narrow — it covers reference sequences that appear repeatedly in
/// numerical recipes, algorithm exercises, and educational material, but it is not a general-purpose recurrence
/// framework. Most of these generators take inclusive bounds over the value space (<c>min</c>, <c>max</c>) rather than
/// element counts, while the Farey and look-and-say overloads accept an order or count parameter where bounds are not
/// meaningful. Refer to each member's documentation for the exact bounding rule.
/// </para>
/// <para>
/// All sequences returned by this type are single-pass with respect to side effects in their generator delegate:
/// re-enumerating the returned <see cref="IEnumerable{T}" /> invokes the supplied delegates again. Callers that require
/// a stable, replayable view should materialize the sequence via <c>ToArray</c> or <c>ToList</c>.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // A counted descending range.
/// foreach (int n in SequenceGenerator.Range(start: 10, stop: 0, step: -2))
///     Console.WriteLine(n); // 10, 8, 6, 4, 2
///
/// // A stateful generator producing powers of two while the value fits in a positive int.
/// IEnumerable<int> powers = SequenceGenerator.NextWhile(
///     initialValue: 1,
///     conditionHandler: value => value > 0,
///     resultSelector: prev => prev * 2);
///
/// // Fibonacci numbers up to 100 — the value bound, not a fixed count.
/// foreach (long fib in SequenceGenerator.Fibonacci(min: 0, max: 100))
///     Console.WriteLine(fib); // 0, 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89
///]]>
/// </example>
public static partial class SequenceGenerator
{
}
