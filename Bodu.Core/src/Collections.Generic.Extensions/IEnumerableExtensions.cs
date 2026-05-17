// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

/// <summary>
/// Provides deferred-execution combinators over <see cref="IEnumerable{T}" /> — batching, caching, multi-aggregate
/// fan-out, recursive descent, randomization, and set-membership predicates — that complement the standard LINQ surface
/// for production pipeline code.
/// </summary>
/// <remarks>
/// <para>
/// LINQ-to-Objects covers the common cases — <c>Where</c>, <c>Select</c>, <c>GroupBy</c>, <c>Aggregate</c> — but most
/// non-trivial pipelines end up reaching for the same hand-rolled helpers: chunking a stream into fixed-size windows,
/// materializing once and replaying many times, walking a tree of children without recursion, or computing several
/// aggregates in a single pass to avoid re-enumerating an expensive source. This class collects those helpers behind
/// verb-led names and shapes them so that they compose naturally with the rest of LINQ.
/// </para>
/// <para>
/// The API surface clusters into: chunking and pooling (<c>Batch</c>, <c>BatchPooled</c>), replay support (<c>Cache</c>
/// ), set-membership predicates (<c>ContainsAll</c>, <c>ContainsAny</c>), tree traversal (<c>RecursiveSelect</c>),
/// shuffling (<c>Randomize</c>), and multi-aggregate fan-out (<c>Aggregate</c> overloads that compute up to several
/// seeds in a single enumeration). Each method is offered with the overload set required to keep the call site clean —
/// a default form, a form that accepts a projection or comparer, and where useful a form that hands the caller a pooled
/// buffer to drive throughput-sensitive work.
/// </para>
/// <para>
/// Where the contract is naturally lazy the implementation defers all work until enumeration begins; where it is
/// naturally eager (the <c>Aggregate</c>, <c>ContainsAll</c>, and <c>ContainsAny</c> families) the source is enumerated
/// exactly once. Argument validation runs eagerly via <see cref="ThrowHelper" />, so callers see
/// <see cref="ArgumentNullException" /> at the call site rather than mid-iteration. Methods that accept an
/// <see cref="IEqualityComparer{T}" /> default to <see cref="EqualityComparer{T}.Default" /> when one is not supplied.
/// </para>
/// <example>
/// <code language="csharp"><![CDATA[ IEnumerable&lt;int&gt; source = Enumerable.Range(1, 9); // Chunk into windows of three.
/// foreach (var window in source.Batch(3)) Console.WriteLine(string.Join(", ", window)); // => 1, 2, 3 // => 4, 5, 6 //
/// => 7, 8, 9 // Compute count and sum in a single pass. var (count, sum) = source.Aggregate( seed1: 0, seed2: 0L,
/// func1: (c, _) =&gt; c + 1, func2: (s, x) =&gt; s + x); // => count == 9, sum == 45 // Predicate-style set check
/// using a custom comparer. bool anyMatch = new[] { "FOO", "bar" } .ContainsAny(new[] { "foo", "baz" },
/// StringComparer.OrdinalIgnoreCase); // => true ]]></code>
/// </example>
/// </remarks>
public static partial class IEnumerableExtensions
{
}