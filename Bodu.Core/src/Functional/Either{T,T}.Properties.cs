// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Either{T,T}.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public readonly partial struct Either<TLeft, TRight>
{
    /// <summary>
    /// Gets a value indicating whether this either carries a left value.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if the left side is active; otherwise, <see langword="false" /> — including for
    /// <c>default(Either&lt;TLeft, TRight&gt;)</c>, which carries neither side.
    /// </value>
    /// <remarks>
    /// <para>
    /// The type deliberately exposes no throwing value accessor; retrieve the active side's value through
    /// <see cref="TryGetLeft(out TLeft)" />, <see cref="TryGetRight(out TRight)" />, or
    /// <see cref="Match{TResult}(Func{TLeft, TResult}, Func{TRight, TResult})" />.
    /// </para>
    /// </remarks>
    public bool IsLeft =>
        _state == StateLeft;

    /// <summary>
    /// Gets a value indicating whether this either carries a right value.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if the right side is active; otherwise, <see langword="false" /> — including for
    /// <c>default(Either&lt;TLeft, TRight&gt;)</c>, which carries neither side.
    /// </value>
    /// <remarks>
    /// <para>
    /// The type deliberately exposes no throwing value accessor; retrieve the active side's value through
    /// <see cref="TryGetLeft(out TLeft)" />, <see cref="TryGetRight(out TRight)" />, or
    /// <see cref="Match{TResult}(Func{TLeft, TResult}, Func{TRight, TResult})" />.
    /// </para>
    /// </remarks>
    public bool IsRight =>
        _state == StateRight;
}
