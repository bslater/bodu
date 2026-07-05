// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Option{T}.IEquatable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public partial struct Option<T>
    : System.IEquatable<Option<T>>
{
    /// <summary>
    /// Determines whether the specified object is equal to the current option.
    /// </summary>
    /// <param name="obj">
    /// The object to compare. Only another <see cref="Option{T}" /> can be equal; all other types, including
    /// <see langword="null" />, return <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is an <see cref="Option{T}" /> equal to this instance;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is Option<T> other && Equals(other);

    /// <summary>
    /// Determines whether the specified option is equal to the current instance.
    /// </summary>
    /// <param name="other">The option to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if both are <c>None</c>, or both carry values that compare equal using
    /// <see cref="EqualityComparer{T}.Default" />; otherwise, <see langword="false" />.
    /// </returns>
    public readonly bool Equals(Option<T> other) =>
        _hasValue
            ? other._hasValue && EqualityComparer<T>.Default.Equals(_value, other._value)
            : !other._hasValue;

    /// <summary>
    /// Returns a hash code for the current instance consistent with <see cref="Equals(Option{T})" />.
    /// </summary>
    /// <returns>The contained value's hash code when present; otherwise <c>0</c>.</returns>
    public override readonly int GetHashCode() =>
        _hasValue && _value is not null ? _value.GetHashCode() : 0;
}
