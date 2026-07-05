// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Either{T,T}.IEquatable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public partial struct Either<TLeft, TRight>
    : System.IEquatable<Either<TLeft, TRight>>
{
    /// <summary>
    /// Determines whether the specified object is equal to the current either.
    /// </summary>
    /// <param name="obj">
    /// The object to compare. Only another <see cref="Either{TLeft, TRight}" /> can be equal; all other types,
    /// including <see langword="null" />, return <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is an <see cref="Either{TLeft, TRight}" /> equal to this
    /// instance; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is Either<TLeft, TRight> other && Equals(other);

    /// <summary>
    /// Determines whether the specified either is equal to the current instance.
    /// </summary>
    /// <param name="other">The either to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if both instances carry the same side and the active side's values compare equal using
    /// <see cref="EqualityComparer{T}.Default" />, or both are uninitialized; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The side participates in equality: <c>Left(x)</c> and <c>Right(x)</c> are never equal, even when
    /// <typeparamref name="TLeft" /> and <typeparamref name="TRight" /> are the same type and the values compare equal.
    /// Two uninitialized (<c>default</c>) instances compare equal.
    /// </para>
    /// </remarks>
    public readonly bool Equals(Either<TLeft, TRight> other) =>
        _state == other._state && _state switch
        {
            StateLeft => EqualityComparer<TLeft>.Default.Equals(_left, other._left),
            StateRight => EqualityComparer<TRight>.Default.Equals(_right, other._right),
            _ => true,
        };

    /// <summary>
    /// Returns a hash code for the current instance consistent with <see cref="Equals(Either{TLeft, TRight})" />.
    /// </summary>
    /// <returns>
    /// A hash code combining the active side with the active side's value, so that <c>Left(x)</c> and <c>Right(x)</c>
    /// hash differently where possible; <c>0</c> for an uninitialized instance.
    /// </returns>
    public override readonly int GetHashCode() =>
        _state switch
        {
            StateLeft => HashCode.Combine(StateLeft, _left),
            StateRight => HashCode.Combine(StateRight, _right),
            _ => 0,
        };
}
