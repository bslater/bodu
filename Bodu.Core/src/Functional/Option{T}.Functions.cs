// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Option{T}.Functions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Functional;

public readonly partial struct Option<T>
{
    /// <summary>
    /// Attempts to retrieve the contained value.
    /// </summary>
    /// <param name="value">
    /// When this method returns, the contained value if one is present; otherwise the default value.
    /// </param>
    /// <returns><see langword="true" /> if a value is present; otherwise, <see langword="false" />.</returns>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return _hasValue;
    }

    /// <summary>
    /// Projects the contained value into a new option using the specified selector.
    /// </summary>
    /// <typeparam name="TResult">The type produced by the selector.</typeparam>
    /// <param name="selector">The projection applied to the contained value.</param>
    /// <returns>
    /// <c>Some(selector(value))</c> when a value is present and the projection is non-<see langword="null" />;
    /// otherwise <c>None</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when this option is <c>None</c>. A <see langword="null" /> projection result maps to
    /// <c>None</c> (the lenient lift), so a mapping chain can never produce a present null.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var length = Option<string>.Some("railway").Map(s => s.Length); // Some(7)
    /// var none = Option<string>.None.Map(s => s.Length);              // None — selector not invoked
    ///]]>
    /// </code>
    /// </example>
    public Option<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(selector);

        return _hasValue ? selector(_value) : Option<TResult>.None;
    }

    /// <summary>
    /// Projects the contained value into another option using the specified option-returning selector.
    /// </summary>
    /// <typeparam name="TResult">The value type of the resulting option.</typeparam>
    /// <param name="selector">The option-returning projection applied to the contained value.</param>
    /// <returns>The option produced by the selector when a value is present; otherwise <c>None</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when this option is <c>None</c>.
    /// </para>
    /// </remarks>
    public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> selector)
    {
        ThrowHelper.ThrowIfNull(selector);

        return _hasValue ? selector(_value) : Option<TResult>.None;
    }

    /// <summary>
    /// Retains the contained value only when it satisfies the specified predicate.
    /// </summary>
    /// <param name="predicate">The condition the contained value must satisfy.</param>
    /// <returns>
    /// This option when a value is present and satisfies <paramref name="predicate" />; otherwise <c>None</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The predicate is not invoked when this option is <c>None</c>.
    /// </para>
    /// </remarks>
    public Option<T> Filter(Func<T, bool> predicate)
    {
        ThrowHelper.ThrowIfNull(predicate);

        return _hasValue && predicate(_value) ? this : None;
    }

    /// <summary>
    /// Collapses the option to a single value by invoking the matching branch.
    /// </summary>
    /// <typeparam name="TResult">The type produced by both branches.</typeparam>
    /// <param name="onSome">The projection invoked with the contained value when one is present.</param>
    /// <param name="onNone">The factory invoked when no value is present.</param>
    /// <returns>The value produced by the invoked branch.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="onSome" /> or <paramref name="onNone" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var label = option.Match(v => $"got {v}", () => "nothing");
    ///]]>
    /// </code>
    /// </example>
    public TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone)
    {
        ThrowHelper.ThrowIfNull(onSome);
        ThrowHelper.ThrowIfNull(onNone);

        return _hasValue ? onSome(_value) : onNone();
    }

    /// <summary>
    /// Invokes the matching action for the state of the option.
    /// </summary>
    /// <param name="onSome">The action invoked with the contained value when one is present.</param>
    /// <param name="onNone">The action invoked when no value is present.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="onSome" /> or <paramref name="onNone" /> is <see langword="null" />.
    /// </exception>
    public void Match(Action<T> onSome, Action onNone)
    {
        ThrowHelper.ThrowIfNull(onSome);
        ThrowHelper.ThrowIfNull(onNone);

        if (_hasValue)
            onSome(_value);
        else
            onNone();
    }

    /// <summary>
    /// Returns the contained value, or the default value of <typeparamref name="T" /> when none is present.
    /// </summary>
    /// <returns>The contained value, or <c>default(T)</c>.</returns>
    public T? GetValueOrDefault() =>
        _hasValue ? _value : default;

    /// <summary>
    /// Returns the contained value, or the specified fallback when none is present.
    /// </summary>
    /// <param name="defaultValue">The value returned when no value is present.</param>
    /// <returns>The contained value, or <paramref name="defaultValue" />.</returns>
    public T GetValueOrDefault(T defaultValue) =>
        _hasValue ? _value : defaultValue;

    /// <summary>
    /// Returns the contained value, or the value produced by the specified factory when none is present.
    /// </summary>
    /// <param name="defaultFactory">The factory invoked to produce the fallback value.</param>
    /// <returns>The contained value, or the factory's result.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="defaultFactory" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The factory is not invoked when a value is present.
    /// </para>
    /// </remarks>
    public T GetValueOrDefault(Func<T> defaultFactory)
    {
        ThrowHelper.ThrowIfNull(defaultFactory);

        return _hasValue ? _value : defaultFactory();
    }
}
