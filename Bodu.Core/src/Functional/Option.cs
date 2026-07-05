// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Option.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

/// <summary>
/// Provides factory methods for creating <see cref="Option{T}" /> values with type inference at the call site.
/// </summary>
/// <remarks>
/// <para>
/// The members mirror the static surface of <see cref="Option{T}" /> so callers can write <c>Option.Some(3)</c> instead
/// of <c>Option&lt;int&gt;.Some(3)</c>, and add <see cref="FromNullable{T}(T?)" /> — the <see cref="Nullable{T}" />
/// lift that the implicit conversion on the generic type cannot express.
/// </para>
/// </remarks>
public static class Option
{
    /// <summary>
    /// Creates an option carrying the specified non-<see langword="null" /> value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to wrap. Must not be <see langword="null" />.</param>
    /// <returns>An option whose <see cref="Option{T}.IsSome" /> is <see langword="true" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public static Option<T> Some<T>(T value) =>
        Option<T>.Some(value);

    /// <summary>
    /// Returns the option of the specified type that carries no value.
    /// </summary>
    /// <typeparam name="T">The value type of the option.</typeparam>
    /// <returns>An option whose <see cref="Option{T}.IsNone" /> is <see langword="true" />.</returns>
    public static Option<T> None<T>() =>
        Option<T>.None;

    /// <summary>
    /// Converts a nullable value type to an option, mapping <see langword="null" /> to <c>None</c>.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="value">The nullable value to lift.</param>
    /// <returns><c>Some(value.Value)</c> when <paramref name="value" /> has a value; otherwise <c>None</c>.</returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// int? parsed = TryParseOrNull(text);
    /// Option<int> option = Option.FromNullable(parsed); // None when parsed is null
    ///]]>
    /// </code>
    /// </example>
    public static Option<T> FromNullable<T>(T? value)
        where T : struct =>
        value.HasValue ? Option<T>.Some(value.Value) : Option<T>.None;
}
