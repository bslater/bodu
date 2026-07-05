// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Result{T}.IEquatable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public partial struct Result<T>
    : System.IEquatable<Result<T>>
{
    /// <summary>
    /// Determines whether the specified object is equal to the current result.
    /// </summary>
    /// <param name="obj">
    /// The object to compare. Only another <see cref="Result{T}" /> can be equal; all other types, including
    /// <see langword="null" />, return <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is a <see cref="Result{T}" /> equal to this instance;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is Result<T> other && Equals(other);

    /// <summary>
    /// Determines whether the specified result is equal to the current instance.
    /// </summary>
    /// <param name="other">The result to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if both are successes carrying values that compare equal using
    /// <see cref="EqualityComparer{T}.Default" />, or both are failures whose errors compare equal using
    /// <see cref="ResultError.Equals(ResultError)" />; otherwise, <see langword="false" />.
    /// </returns>
    public readonly bool Equals(Result<T> other) =>
        _isSuccess
            ? other._isSuccess && EqualityComparer<T>.Default.Equals(_value, other._value)
            : !other._isSuccess && _error.Equals(other._error);

    /// <summary>
    /// Returns a hash code for the current instance consistent with <see cref="Equals(Result{T})" />.
    /// </summary>
    /// <returns>The carried value's hash code for success; otherwise the error's hash code.</returns>
    public override readonly int GetHashCode() =>
        _isSuccess
            ? _value is not null ? _value.GetHashCode() : 0
            : _error.GetHashCode();
}
