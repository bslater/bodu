// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Result{T}.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public readonly partial struct Result<T>
{
    /// <summary>
    /// Determines whether two results are equal.
    /// </summary>
    /// <param name="left">The first result to compare.</param>
    /// <param name="right">The second result to compare.</param>
    /// <returns>
    /// <see langword="true" /> if both are successes carrying values that compare equal using the default equality
    /// comparer, or both are failures carrying equal errors; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator ==(Result<T> left, Result<T> right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two results are not equal.
    /// </summary>
    /// <param name="left">The first result to compare.</param>
    /// <param name="right">The second result to compare.</param>
    /// <returns><see langword="true" /> if the results differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(Result<T> left, Result<T> right) =>
        !left.Equals(right);
}
