// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Either{T,T}.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public readonly partial struct Either<TLeft, TRight>
{
    /// <summary>
    /// Determines whether two eithers are equal.
    /// </summary>
    /// <param name="left">The first either to compare.</param>
    /// <param name="right">The second either to compare.</param>
    /// <returns>
    /// <see langword="true" /> if both carry the same side with values that compare equal using the default equality
    /// comparer, or both are uninitialized; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator ==(Either<TLeft, TRight> left, Either<TLeft, TRight> right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two eithers are not equal.
    /// </summary>
    /// <param name="left">The first either to compare.</param>
    /// <param name="right">The second either to compare.</param>
    /// <returns><see langword="true" /> if the eithers differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(Either<TLeft, TRight> left, Either<TLeft, TRight> right) =>
        !left.Equals(right);
}
