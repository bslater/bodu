// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.NextWhile.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Yields a value-driven sequence by repeatedly applying a successor function as long as a predicate continues to
    /// hold.
    /// </summary>
    /// <typeparam name="TResult">The element type produced by the sequence.</typeparam>
    /// <param name="initialValue">The seed value emitted as the first element of the sequence.</param>
    /// <param name="conditionHandler">
    /// A predicate evaluated against the current value before each emission. Must not be <see langword="null" />.
    /// </param>
    /// <param name="resultSelector">
    /// A function that derives the next value from the current value. Must not be <see langword="null" />.
    /// </param>
    /// <returns>
    /// A lazily evaluated sequence terminating on the first iteration where <paramref name="conditionHandler" />
    /// returns <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="conditionHandler" /> or <paramref name="resultSelector" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this overload when the next value depends only on the current one — classic recurrence relations such as
    /// <c>x ↦ 2·x</c> or <c>x ↦ x / 2</c>. Reach for the indexed overload when the position matters, and the
    /// state-based overload when more than one variable must be tracked across iterations.
    /// </para>
    /// <para>
    /// The condition is checked before each yield, so the sequence terminates as soon as the predicate fails —
    /// including on the very first element, which produces an empty sequence. Behavior is fully deterministic provided
    /// <paramref name="conditionHandler" /> and <paramref name="resultSelector" /> are themselves pure.
    /// </para>
    /// <para>
    /// The sequence may be infinite if the predicate never fails; bound it with <c>Take</c> or <c>TakeWhile</c> when an
    /// upper limit is required. Iteration is deferred and allocates only the iterator state and a single boxed state
    /// slot.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Halving sequence — terminates when the value drops to zero.
    /// var halves = SequenceGenerator.NextWhile(64, v => v > 0, v => v / 2); // => 64, 32, 16, 8, 4, 2, 1
    ///
    /// // Empty result when the seed already fails the predicate.
    /// var none = SequenceGenerator.NextWhile(0, v => v > 0, v => v - 1); // => (empty)
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<TResult> NextWhile<TResult>(
        TResult initialValue,
        Func<TResult, bool> conditionHandler,
        Func<TResult, TResult> resultSelector)
    {
        ThrowHelper.ThrowIfNull(conditionHandler);
        ThrowHelper.ThrowIfNull(resultSelector);

        return NextWhile(
            new State<TResult> { Value = initialValue },
            state => conditionHandler(state.Value),
            state =>
            {
                state.Value = resultSelector(state.Value);
                return state;
            },
            state => state.Value);
    }

    /// <summary>
    /// Yields a value-driven sequence whose successor function additionally receives the zero-based index of the
    /// current element.
    /// </summary>
    /// <typeparam name="TResult">The element type produced by the sequence.</typeparam>
    /// <param name="initialValue">The seed value emitted as the first element of the sequence.</param>
    /// <param name="conditionHandler">
    /// A predicate evaluated against the current value before each emission. Must not be <see langword="null" />.
    /// </param>
    /// <param name="resultSelector">
    /// A function that derives the next value from the current value and its zero-based index. The index passed in for
    /// the very first call to the selector is <c>0</c>. Must not be <see langword="null" />.
    /// </param>
    /// <returns>
    /// A lazily evaluated sequence terminating on the first iteration where <paramref name="conditionHandler" />
    /// returns <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="conditionHandler" /> or <paramref name="resultSelector" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Choose this overload over
    /// <see cref="NextWhile{TResult}(TResult, Func{TResult, bool}, Func{TResult, TResult})" /> when the transformation
    /// depends on its position — for example, accumulating sums of <c>i</c>, generating polynomial terms, or scaling by
    /// a power of the current index.
    /// </para>
    /// <para>
    /// The index increments after the selector returns, so the seed is index <c>0</c>, the value passed to the first
    /// selector call is index <c>0</c>, and the value emitted next sits at index <c>1</c>. Iteration is deferred and
    /// the index is held internally; it cannot overflow a single iteration but will wrap to <see cref="int.MinValue" />
    /// after <see cref="int.MaxValue" /> elements.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Triangular numbers up to 100: 0, 1, 3, 6, 10, 15, 21, 28, 36, 45, 55, 66, 78, 91.
    /// var triangular = SequenceGenerator.NextWhile(0, v => v <= 100, (v, i) => v + (i + 1));
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<TResult> NextWhile<TResult>(
        TResult initialValue,
        Func<TResult, bool> conditionHandler,
        Func<TResult, int, TResult> resultSelector)
    {
        ThrowHelper.ThrowIfNull(conditionHandler);
        ThrowHelper.ThrowIfNull(resultSelector);

        return NextWhile(
            new State<TResult> { Value = initialValue },
            state => conditionHandler(state.Value),
            state =>
            {
                state.Value = resultSelector(state.Value, state.Index++);
                return state;
            },
            state => state.Value);
    }

    /// <summary>
    /// Yields a sequence projected from a custom state object that is advanced on each iteration.
    /// </summary>
    /// <typeparam name="TState">The shape of the iteration state.</typeparam>
    /// <typeparam name="TResult">The element type produced by the sequence.</typeparam>
    /// <param name="initialState">The initial state used as the iteration seed.</param>
    /// <param name="conditionHandler">
    /// A predicate evaluated against the current state before each emission. Must not be <see langword="null" />.
    /// </param>
    /// <param name="iterateFunction">
    /// A function that produces the next state from the current state. Must not be <see langword="null" />.
    /// </param>
    /// <param name="resultSelector">
    /// A function that projects the current state into the emitted element. Must not be <see langword="null" />.
    /// </param>
    /// <returns>
    /// A lazily evaluated sequence whose elements are produced by projecting each state visited by the iteration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="conditionHandler" />, <paramref name="iterateFunction" />, or
    /// <paramref name="resultSelector" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the most general <c>NextWhile</c> form. Use it when each iteration needs to track several variables —
    /// for example, a pair of counters, a running accumulator, or a parser state — that cannot be folded into a single
    /// value.
    /// </para>
    /// <para>
    /// The state object is opaque to <see cref="SequenceGenerator" />: if <typeparamref name="TState" /> is mutable the
    /// caller is responsible for the mutation discipline, and if it is a struct each call to
    /// <paramref name="iterateFunction" /> may need to return an updated copy. Iteration is deferred and produces no
    /// allocations beyond the iterator state itself.
    /// </para>
    /// <para>
    /// As with the other overloads, the predicate is evaluated before each emission, the sequence may be infinite if it
    /// never fails, and behavior is deterministic provided the supplied delegates are pure with respect to the state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Fibonacci numbers below 100, tracked through a (prev, curr) state record.
    /// var fib = SequenceGenerator.NextWhile(
    ///     initialState: (Prev: 0, Curr: 1),
    ///     conditionHandler: s => s.Curr < 100,
    ///     iterateFunction: s => (s.Curr, s.Prev + s.Curr),
    ///     resultSelector: s => s.Curr); // => 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<TResult> NextWhile<TState, TResult>(
        TState initialState,
        Func<TState, bool> conditionHandler,
        Func<TState, TState> iterateFunction,
        Func<TState, TResult> resultSelector)
    {
        ThrowHelper.ThrowIfNull(conditionHandler);
        ThrowHelper.ThrowIfNull(iterateFunction);
        ThrowHelper.ThrowIfNull(resultSelector);

        IEnumerable<TResult> Iterator()
        {
            for (TState state = initialState; conditionHandler(state); state = iterateFunction(state))
            {
                yield return resultSelector(state);
            }
        }

        return Iterator();
    }

    private struct State<T>
    {
        public int Index;
        public T Value;
    }
}
