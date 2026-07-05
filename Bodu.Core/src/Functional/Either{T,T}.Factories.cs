// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Either{T,T}.Factories.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public readonly partial struct Either<TLeft, TRight>
{
    /// <summary>
    /// Creates an either carrying the specified non-<see langword="null" /> left value.
    /// </summary>
    /// <param name="value">The left value to wrap. Must not be <see langword="null" />.</param>
    /// <returns>
    /// An <see cref="Either{TLeft, TRight}" /> whose <see cref="IsLeft" /> is <see langword="true" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The factory rejects <see langword="null" /> because a side that is present must carry a usable value; absence is
    /// modelled by <see cref="Option{T}" />, not by a null side.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var left = Either<int, string>.Left(42);        // Left(42)
    /// var right = Either<int, string>.Right("text");  // Right(text)
    ///]]>
    /// </code>
    /// </example>
    public static Either<TLeft, TRight> Left(TLeft value)
    {
        ThrowHelper.ThrowIfNull(value);

        return new Either<TLeft, TRight>(value);
    }

    /// <summary>
    /// Creates an either carrying the specified non-<see langword="null" /> right value.
    /// </summary>
    /// <param name="value">The right value to wrap. Must not be <see langword="null" />.</param>
    /// <returns>
    /// An <see cref="Either{TLeft, TRight}" /> whose <see cref="IsRight" /> is <see langword="true" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The factory rejects <see langword="null" /> because a side that is present must carry a usable value; absence is
    /// modelled by <see cref="Option{T}" />, not by a null side.
    /// </para>
    /// </remarks>
    public static Either<TLeft, TRight> Right(TRight value)
    {
        ThrowHelper.ThrowIfNull(value);

        return new Either<TLeft, TRight>(value);
    }
}
