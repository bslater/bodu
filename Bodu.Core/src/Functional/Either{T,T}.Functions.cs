// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Either{T,T}.Functions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Functional;

public readonly partial struct Either<TLeft, TRight>
{
    /// <summary>
    /// Attempts to retrieve the left value.
    /// </summary>
    /// <param name="value">
    /// When this method returns, the left value if the left side is active; otherwise the default value.
    /// </param>
    /// <returns><see langword="true" /> if the left side is active; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// <para>
    /// Returns <see langword="false" /> — and never throws — for a right-carrying either and for
    /// <c>default(Either&lt;TLeft, TRight&gt;)</c>.
    /// </para>
    /// </remarks>
    public bool TryGetLeft([MaybeNullWhen(false)] out TLeft value)
    {
        value = _left;
        return _state == StateLeft;
    }

    /// <summary>
    /// Attempts to retrieve the right value.
    /// </summary>
    /// <param name="value">
    /// When this method returns, the right value if the right side is active; otherwise the default value.
    /// </param>
    /// <returns><see langword="true" /> if the right side is active; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// <para>
    /// Returns <see langword="false" /> — and never throws — for a left-carrying either and for
    /// <c>default(Either&lt;TLeft, TRight&gt;)</c>.
    /// </para>
    /// </remarks>
    public bool TryGetRight([MaybeNullWhen(false)] out TRight value)
    {
        value = _right;
        return _state == StateRight;
    }

    /// <summary>
    /// Collapses the either to a single value by invoking the branch for the active side.
    /// </summary>
    /// <typeparam name="TResult">The type produced by both branches.</typeparam>
    /// <param name="onLeft">The projection invoked with the left value when the left side is active.</param>
    /// <param name="onRight">The projection invoked with the right value when the right side is active.</param>
    /// <returns>The value produced by the invoked branch.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="onLeft" /> or <paramref name="onRight" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The either is <c>default(Either&lt;TLeft, TRight&gt;)</c> and carries neither side.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var label = either.Match(l => $"number {l}", r => $"text {r}");
    ///]]>
    /// </code>
    /// </example>
    public TResult Match<TResult>(Func<TLeft, TResult> onLeft, Func<TRight, TResult> onRight)
    {
        ThrowHelper.ThrowIfNull(onLeft);
        ThrowHelper.ThrowIfNull(onRight);
        if (_state == StateUninitialized) throw new InvalidOperationException(ResourceStrings.Op_Invalid_EitherUninitialized);

        return _state == StateLeft ? onLeft(_left) : onRight(_right);
    }

    /// <summary>
    /// Invokes the action for the active side of the either.
    /// </summary>
    /// <param name="onLeft">The action invoked with the left value when the left side is active.</param>
    /// <param name="onRight">The action invoked with the right value when the right side is active.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="onLeft" /> or <paramref name="onRight" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The either is <c>default(Either&lt;TLeft, TRight&gt;)</c> and carries neither side.
    /// </exception>
    public void Match(Action<TLeft> onLeft, Action<TRight> onRight)
    {
        ThrowHelper.ThrowIfNull(onLeft);
        ThrowHelper.ThrowIfNull(onRight);
        if (_state == StateUninitialized) throw new InvalidOperationException(ResourceStrings.Op_Invalid_EitherUninitialized);

        if (_state == StateLeft)
            onLeft(_left);
        else
            onRight(_right);
    }

    /// <summary>
    /// Projects the left value using the specified selector, passing a right value through untouched.
    /// </summary>
    /// <typeparam name="TResult">The left type produced by the selector.</typeparam>
    /// <param name="selector">The projection applied to the left value when the left side is active.</param>
    /// <returns>
    /// <c>Left(selector(value))</c> when the left side is active; otherwise a right-carrying either with the original
    /// right value.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// The either is <c>default(Either&lt;TLeft, TRight&gt;)</c> and carries neither side.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the right side is active. The lift is strict: a <see langword="null" />
    /// projection result is rejected by the <see cref="Either{TResult, TRight}.Left(TResult)" /> factory with
    /// <see cref="ArgumentNullException" />.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var doubled = Either<int, string>.Left(21).MapLeft(l => l * 2);   // Left(42)
    /// var same = Either<int, string>.Right("text").MapLeft(l => l * 2); // Right(text) — selector not invoked
    ///]]>
    /// </code>
    /// </example>
    public Either<TResult, TRight> MapLeft<TResult>(Func<TLeft, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(selector);
        if (_state == StateUninitialized) throw new InvalidOperationException(ResourceStrings.Op_Invalid_EitherUninitialized);

        return _state == StateLeft
            ? Either<TResult, TRight>.Left(selector(_left))
            : Either<TResult, TRight>.Right(_right);
    }

    /// <summary>
    /// Projects the right value using the specified selector, passing a left value through untouched.
    /// </summary>
    /// <typeparam name="TResult">The right type produced by the selector.</typeparam>
    /// <param name="selector">The projection applied to the right value when the right side is active.</param>
    /// <returns>
    /// <c>Right(selector(value))</c> when the right side is active; otherwise a left-carrying either with the original
    /// left value.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// The either is <c>default(Either&lt;TLeft, TRight&gt;)</c> and carries neither side.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the left side is active. The lift is strict: a <see langword="null" />
    /// projection result is rejected by the <see cref="Either{TLeft, TResult}.Right(TResult)" /> factory with
    /// <see cref="ArgumentNullException" />.
    /// </para>
    /// </remarks>
    public Either<TLeft, TResult> MapRight<TResult>(Func<TRight, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(selector);
        if (_state == StateUninitialized) throw new InvalidOperationException(ResourceStrings.Op_Invalid_EitherUninitialized);

        return _state == StateRight
            ? Either<TLeft, TResult>.Right(selector(_right))
            : Either<TLeft, TResult>.Left(_left);
    }

    /// <summary>
    /// Exchanges the sides of the either.
    /// </summary>
    /// <returns>
    /// An <see cref="Either{TRight, TLeft}" /> carrying the same value on the opposite side: a left value becomes the
    /// right value and a right value becomes the left value.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The either is <c>default(Either&lt;TLeft, TRight&gt;)</c> and carries neither side.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Swapping twice yields an either equal to the original.
    /// </para>
    /// </remarks>
    public Either<TRight, TLeft> Swap()
    {
        if (_state == StateUninitialized) throw new InvalidOperationException(ResourceStrings.Op_Invalid_EitherUninitialized);

        return _state == StateLeft
            ? Either<TRight, TLeft>.Right(_left)
            : Either<TRight, TLeft>.Left(_right);
    }
}
