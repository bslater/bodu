// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Option{T}.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public readonly partial struct Option<T>
{
    /// <summary>
    /// Determines whether two options are equal.
    /// </summary>
    /// <param name="left">The first option to compare.</param>
    /// <param name="right">The second option to compare.</param>
    /// <returns>
    /// <see langword="true" /> if both are <c>None</c>, or both carry values that compare equal using the default
    /// equality comparer; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator ==(Option<T> left, Option<T> right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two options are not equal.
    /// </summary>
    /// <param name="left">The first option to compare.</param>
    /// <param name="right">The second option to compare.</param>
    /// <returns><see langword="true" /> if the options differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(Option<T> left, Option<T> right) =>
        !left.Equals(right);

    /// <summary>
    /// Converts a value to an option, mapping <see langword="null" /> to <c>None</c>.
    /// </summary>
    /// <param name="value">The value to lift.</param>
    /// <returns>
    /// <c>Some(value)</c> when <paramref name="value" /> is not <see langword="null" />; otherwise <c>None</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the lenient lift: <see langword="null" /> means absence. To treat <see langword="null" /> as a bug
    /// instead, use the strict <see cref="Some(T)" /> factory, which throws <see cref="ArgumentNullException" />. To
    /// lift a <see cref="Nullable{T}" /> value type, use <c>Option.FromNullable</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// string? found = lookup.TryGet(key);
    /// Option<string> option = found; // null becomes None, a value becomes Some
    ///]]>
    /// </code>
    /// </example>
    public static implicit operator Option<T>(T? value) =>
        value is null ? None : new Option<T>(value);
}
