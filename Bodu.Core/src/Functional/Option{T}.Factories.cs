// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Option{T}.Factories.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public readonly partial struct Option<T>
{
    /// <summary>
    /// Gets the option that carries no value.
    /// </summary>
    /// <value>
    /// An <see cref="Option{T}" /> whose <see cref="IsNone" /> is <see langword="true" />. Equal to
    /// <c>default(Option&lt;T&gt;)</c>.
    /// </value>
    public static Option<T> None =>
        default;

    /// <summary>
    /// Creates an option carrying the specified non-<see langword="null" /> value.
    /// </summary>
    /// <param name="value">The value to wrap. Must not be <see langword="null" />.</param>
    /// <returns>An <see cref="Option{T}" /> whose <see cref="IsSome" /> is <see langword="true" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// This is the strict lift: a <see langword="null" /> argument is rejected because a present null value defeats the
    /// purpose of the type. To map <see langword="null" /> to <see cref="None" /> instead, use the implicit conversion
    /// from <typeparamref name="T" /> (the lenient lift).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var some = Option<string>.Some("value"); // Some(value)
    /// var none = Option<string>.None;          // None
    ///]]>
    /// </code>
    /// </example>
    public static Option<T> Some(T value)
    {
        ThrowHelper.ThrowIfNull(value);

        return new Option<T>(value);
    }
}
