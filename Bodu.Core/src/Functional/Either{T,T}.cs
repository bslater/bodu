// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Either{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Functional;

/// <summary>
/// Represents a disjoint union of two possibilities: either <c>Left(value)</c> carrying a non-<see langword="null" />
/// value of type <typeparamref name="TLeft" />, or <c>Right(value)</c> carrying a non-<see langword="null" /> value of
/// type <typeparamref name="TRight" />.
/// </summary>
/// <typeparam name="TLeft">The type of the value carried on the left side.</typeparam>
/// <typeparam name="TRight">The type of the value carried on the right side.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Either{TLeft, TRight}" /> is a symmetric choice type: neither side is privileged, and no side carries a
/// success or failure connotation — <see cref="Result{T}" /> owns the railway-oriented bias. Use it when a value is
/// legitimately one of two shapes (for example, a parsed literal that is either a number or a string). Project each
/// side independently with <see cref="MapLeft{TResult}(Func{TLeft, TResult})" /> and
/// <see cref="MapRight{TResult}(Func{TRight, TResult})" />, exchange the sides with <see cref="Swap" />, and collapse
/// to a single value with <see cref="Match{TResult}(Func{TLeft, TResult}, Func{TRight, TResult})" />.
/// </para>
/// <para>
/// <c>default(Either&lt;TLeft, TRight&gt;)</c> carries neither side. On an uninitialized instance <see cref="IsLeft" />
/// and <see cref="IsRight" /> are both <see langword="false" />, <see cref="TryGetLeft(out TLeft)" /> and
/// <see cref="TryGetRight(out TRight)" /> return <see langword="false" />, and every operation that requires a side —
/// <c>Match</c>, <c>MapLeft</c>, <c>MapRight</c>, and <c>Swap</c> — throws <see cref="InvalidOperationException" />.
/// </para>
/// <para>
/// The type throws on <c>default</c> rather than defaulting to a side because no privileged side exists to default to —
/// unlike <see cref="Option{T}" />, whose <c>default</c> is <c>None</c>, and <see cref="Result{T}" />, whose
/// <c>default</c> is a failure carrying the empty error. Silently treating <c>default</c> as <c>Left(default!)</c>
/// would fabricate a value the <see cref="Left(TLeft)" /> factory refuses to accept. This mirrors the
/// <see cref="System.Collections.Immutable.ImmutableArray{T}.IsDefault" /> precedent for a struct whose uninitialized
/// state is observable but unusable.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly partial struct Either<TLeft, TRight>
{
    /// <summary>The state value indicating an uninitialized instance (<c>default</c>).</summary>
    private const byte StateUninitialized = 0;

    /// <summary>The state value indicating the left side is active.</summary>
    private const byte StateLeft = 1;

    /// <summary>The state value indicating the right side is active.</summary>
    private const byte StateRight = 2;

    /// <summary>The active side: <see cref="StateLeft" /> or <see cref="StateRight" />, or <see cref="StateUninitialized" /> for <c>default(Either&lt;TLeft, TRight&gt;)</c>.</summary>
    private readonly byte _state;

    /// <summary>The left value when <see cref="_state" /> is <see cref="StateLeft" />; otherwise <c>default(TLeft)</c> and never observed.</summary>
    private readonly TLeft _left;

    /// <summary>The right value when <see cref="_state" /> is <see cref="StateRight" />; otherwise <c>default(TRight)</c> and never observed.</summary>
    private readonly TRight _right;

    /// <summary>
    /// Initializes a new instance of the <see cref="Either{TLeft, TRight}" /> struct carrying a left value.
    /// </summary>
    /// <param name="left">
    /// The left value to store. The caller has already validated it is not <see langword="null" />.
    /// </param>
    private Either(TLeft left)
    {
        _state = StateLeft;
        _left = left;
        _right = default!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Either{TLeft, TRight}" /> struct carrying a right value.
    /// </summary>
    /// <param name="right">
    /// The right value to store. The caller has already validated it is not <see langword="null" />.
    /// </param>
    private Either(TRight right)
    {
        _state = StateRight;
        _left = default!;
        _right = right;
    }
}
