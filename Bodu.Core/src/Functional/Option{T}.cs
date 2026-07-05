// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Option{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Functional;

/// <summary>
/// Represents an optional value: either <c>Some(value)</c> carrying a non-<see langword="null" /> value of type
/// <typeparamref name="T" />, or <c>None</c> carrying nothing.
/// </summary>
/// <typeparam name="T">The type of the contained value.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Option{T}" /> makes the possible absence of a value explicit in a signature, replacing
/// <see langword="null" /> returns and <c>bool Try…(out …)</c> pairs with a single composable value. Chain
/// transformations with <see cref="Map{TResult}(Func{T, TResult})" /> and
/// <see cref="Bind{TResult}(Func{T, Option{TResult}})" />, and collapse to a final value with
/// <see cref="Match{TResult}(Func{T, TResult}, Func{TResult})" /> or the <c>GetValueOrDefault</c> overloads.
/// </para>
/// <para>
/// <c>default(Option&lt;T&gt;)</c> equals <see cref="None" /> — an option that was never assigned behaves exactly like
/// an explicit <c>None</c>, so the type is total and safe to use as a field or array element without initialization.
/// </para>
/// <para>
/// The type distinguishes a <em>strict</em> and a <em>lenient</em> lift: <see cref="Some(T)" /> rejects
/// <see langword="null" /> with <see cref="ArgumentNullException" /> (a present null is treated as a bug), while the
/// implicit conversion from <typeparamref name="T" /> maps <see langword="null" /> to <see cref="None" /> (absence).
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly partial struct Option<T>
{
    /// <summary>Indicates whether this instance carries a value. <see langword="false" /> for <see cref="None" /> and for <c>default(Option&lt;T&gt;)</c>.</summary>
    private readonly bool _hasValue;

    /// <summary>The contained value when <see cref="_hasValue" /> is <see langword="true" />; otherwise <c>default(T)</c> and never observed.</summary>
    private readonly T _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Option{T}" /> struct carrying the specified value.
    /// </summary>
    /// <param name="value">
    /// The value to store. The caller has already validated it is not <see langword="null" />.
    /// </param>
    private Option(T value)
    {
        _hasValue = true;
        _value = value;
    }
}
