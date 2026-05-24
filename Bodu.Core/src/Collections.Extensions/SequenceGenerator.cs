// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Extensions;

/// <summary>
/// Provides lazily evaluated generators for well-known mathematical sequences — Fibonacci, Farey, Leibniz,
/// look-and-say, and Thue–Morse.
/// </summary>
/// <remarks>
/// <para>
/// Each generator returns a deferred <see cref="IEnumerable{T}" /> that produces elements on demand. Nothing is
/// materialized up front; iteration cost is paid per consumed element, allowing infinite or value-bounded sequences to
/// be composed with LINQ operators such as <c>Take</c>, <c>Where</c>, and <c>Select</c> without intermediate
/// allocation.
/// </para>
/// <para>
/// The catalogue is intentionally narrow — it covers reference sequences that appear repeatedly in numerical recipes,
/// algorithm exercises, and educational material, but it is not a general-purpose recurrence framework. For arbitrary
/// state-machine-style generators (custom recurrences, transition functions, predicate-bounded loops), use the
/// <see cref="Bodu.Collections.Generic.SequenceGenerator" /> sibling class, which exposes the underlying
/// <c>NextWhile</c>, <c>Range</c>, and <c>Repeat</c> primitives that this catalogue is composed against.
/// </para>
/// <para>
/// Most generators take inclusive bounds (<c>min</c>, <c>max</c>) over the value space rather than element counts. The
/// Farey and look-and-say overloads accept an order or count parameter where bounds are not meaningful. Refer to each
/// member's documentation for the exact bounding rule.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Fibonacci numbers up to 100 — the value bound, not a fixed count.
/// foreach (long fib in SequenceGenerator.Fibonacci(min: 0, max: 100))
///     Console.WriteLine(fib); // 0, 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89
///
/// // First five Farey-sequence orders of 1/2 .. 1/1.
/// foreach ((int n, int d) in SequenceGenerator.Farey(order: 5))
///     Console.WriteLine($"{n}/{d}");
///
/// // Look-and-say from "1": "1", "11", "21", "1211", "111221" ...
/// string[] lookAndSay = SequenceGenerator.LookAndSay(count: 5).ToArray();
///]]>
/// </example>
public static partial class SequenceGenerator
{ }
