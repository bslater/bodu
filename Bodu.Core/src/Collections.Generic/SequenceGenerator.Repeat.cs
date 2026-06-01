// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.Repeat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Yields the same value repeatedly, without an upper bound.
    /// </summary>
    /// <typeparam name="T">The element type produced by the sequence.</typeparam>
    /// <param name="value">
    /// The value emitted on every iteration. May be <see langword="null" /> when <typeparamref name="T" /> is a
    /// reference or nullable value type.
    /// </param>
    /// <returns>
    /// An infinite, lazily evaluated sequence in which every element is reference- or value-equal to
    /// <paramref name="value" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this overload when a caller needs an unbounded constant feed — typically composed with operators such as
    /// <c>Take</c>, <c>TakeWhile</c>, or <c>Zip</c>. When a fixed length is required up-front, prefer the
    /// <see cref="Repeat{T}(T, int)" /> overload or <see cref="System.Linq.Enumerable.Repeat{TResult}(TResult, int)" />
    /// .
    /// </para>
    /// <para>
    /// The sequence is never materialized: the iterator yields the same captured reference (or value) on every call to
    /// <c>MoveNext</c>, so memory usage is constant regardless of how many elements the consumer reads.
    /// </para>
    /// <para>
    /// Enumerating the result without a terminating operator will loop indefinitely. The iterator itself is not
    /// thread-safe; obtain a separate enumerator per consumer.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// Generate the first five powers of two by zipping a counter against a constant feed.
    /// var powers = SequenceGenerator.Repeat(2)
    ///     .Zip(SequenceGenerator.Range(0, 4), (b, e) => (long)Math.Pow(b, e)); // => 1, 2, 4, 8, 16
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<T> Repeat<T>(T value)
    {
        while (true)
            yield return value;
    }

    /// <summary>
    /// Yields the specified value a fixed number of times.
    /// </summary>
    /// <typeparam name="T">The element type produced by the sequence.</typeparam>
    /// <param name="value">
    /// The value emitted on every iteration. May be <see langword="null" /> when <typeparamref name="T" /> is a
    /// reference or nullable value type.
    /// </param>
    /// <param name="count">The number of times to emit <paramref name="value" />. Must be non-negative.</param>
    /// <returns>
    /// A lazily evaluated, finite sequence containing exactly <paramref name="count" /> copies of
    /// <paramref name="value" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    /// <remarks>
    /// <para>
    /// Functionally equivalent to <see cref="System.Linq.Enumerable.Repeat{TResult}(TResult, int)" />; prefer this
    /// overload when the rest of the pipeline already lives in <see cref="SequenceGenerator" /> for stylistic
    /// consistency, or when interoperating with the unbounded <see cref="Repeat{T}(T)" /> form. The BCL alternative is
    /// otherwise equivalent in behavior and performance.
    /// </para>
    /// <para>
    /// A <paramref name="count" /> of <c>0</c> returns an empty sequence and never invokes the iterator beyond its
    /// initial setup. Iteration is deferred — argument validation runs immediately, but the body executes only once
    /// enumeration begins (after the first <c>MoveNext</c> call due to the <see cref="ThrowHelper" /> check inside the
    /// iterator method).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var dashes = string.Concat(SequenceGenerator.Repeat('-', 5)); // => "-----"
    /// var padding = SequenceGenerator.Repeat<string?>(null, 3).ToArray(); // => [null, null, null]
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<T> Repeat<T>(T value, int count)
    {
        ThrowHelper.ThrowIfLessThan(count, 0);

        for (var i = 0; i < count; i++)
            yield return value;
    }
}
