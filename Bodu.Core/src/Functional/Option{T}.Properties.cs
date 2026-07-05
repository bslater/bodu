// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Option{T}.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public readonly partial struct Option<T>
{
    /// <summary>
    /// Gets a value indicating whether this option carries a value.
    /// </summary>
    /// <value><see langword="true" /> if a value is present; otherwise, <see langword="false" />.</value>
    public bool IsSome =>
        _hasValue;

    /// <summary>
    /// Gets a value indicating whether this option carries no value.
    /// </summary>
    /// <value><see langword="true" /> if no value is present; otherwise, <see langword="false" />.</value>
    public bool IsNone =>
        !_hasValue;

    /// <summary>
    /// Gets the contained value.
    /// </summary>
    /// <value>The value carried by this option.</value>
    /// <exception cref="InvalidOperationException">The option does not contain a value.</exception>
    /// <remarks>
    /// <para>
    /// Prefer <see cref="TryGetValue(out T)" />, <see cref="Match{TResult}(Func{T, TResult}, Func{TResult})" />, or the
    /// <c>GetValueOrDefault</c> overloads over direct access when absence is an expected state; <see cref="Value" /> is
    /// intended for call sites that have already established <see cref="IsSome" />.
    /// </para>
    /// </remarks>
    public T Value =>
        _hasValue ? _value : throw new InvalidOperationException(ResourceStrings.Op_Invalid_OptionHasNoValue);
}
