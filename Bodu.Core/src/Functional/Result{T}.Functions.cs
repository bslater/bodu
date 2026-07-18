// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Result{T}.Functions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Functional;

public readonly partial struct Result<T>
{
    /// <summary>
    /// Attempts to retrieve the value carried by a successful result.
    /// </summary>
    /// <param name="value">
    /// When this method returns, the value if the result represents success; otherwise the default value.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the result represents success; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return _isSuccess;
    }

    /// <summary>
    /// Attempts to retrieve the error carried by a failed result.
    /// </summary>
    /// <param name="error">
    /// When this method returns, the error if the result represents failure; otherwise the default error.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the result represents failure; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetError(out ResultError error)
    {
        error = _error;
        return !_isSuccess;
    }

    /// <summary>
    /// Projects the carried value into a new result using the specified selector.
    /// </summary>
    /// <typeparam name="TResult">The type produced by the selector.</typeparam>
    /// <param name="selector">The projection applied to the carried value.</param>
    /// <returns>
    /// <c>Success(selector(value))</c> when the result represents success; otherwise a failure carrying the original
    /// error.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="selector" /> is <see langword="null" />, or the projection returned <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when this result is a failure. The projected value is lifted through the strict
    /// <see cref="Result.Success{T}(T)" /> factory, so a <see langword="null" /> projection result throws
    /// <see cref="ArgumentNullException" /> — a successful result can never carry <see langword="null" />.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var length = Result.Success("railway").Map(s => s.Length); // Success(7)
    /// var failed = Result.Failure<string>("boom").Map(s => s.Length); // Failure(boom) — selector not invoked
    ///]]>
    /// </code>
    /// </example>
    public Result<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(selector);

        return _isSuccess ? Result.Success(selector(_value)) : Result<TResult>.CreateFailure(_error);
    }

    /// <summary>
    /// Projects the carried value into another result using the specified result-returning selector.
    /// </summary>
    /// <typeparam name="TResult">The value type of the resulting result.</typeparam>
    /// <param name="selector">The result-returning projection applied to the carried value.</param>
    /// <returns>
    /// The result produced by the selector when this result represents success; otherwise a failure carrying the
    /// original error.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when this result is a failure.
    /// </para>
    /// </remarks>
    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> selector)
    {
        ThrowHelper.ThrowIfNull(selector);

        return _isSuccess ? selector(_value) : Result<TResult>.CreateFailure(_error);
    }

    /// <summary>
    /// Demotes a success to a failure carrying the specified error when the carried value does not satisfy the
    /// specified predicate.
    /// </summary>
    /// <param name="predicate">The condition the carried value must satisfy.</param>
    /// <param name="error">The error carried by the failure produced when the predicate is not satisfied.</param>
    /// <returns>
    /// This result when it is already a failure or when a value is present and satisfies <paramref name="predicate" />;
    /// otherwise a failure carrying <paramref name="error" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The predicate is not invoked when this result is already a failure; the original error is preserved unchanged.
    /// This is the failure-shaped counterpart of <see cref="Option{T}.Filter(Func{T, bool})" />, which drops a filtered
    /// value to <c>None</c>: here the caller names the error that the filtered-out value represents.
    /// </para>
    /// </remarks>
    public Result<T> Filter(Func<T, bool> predicate, ResultError error)
    {
        ThrowHelper.ThrowIfNull(predicate);

        if (!_isSuccess || predicate(_value))
            return this;

        return CreateFailure(error);
    }

    /// <summary>
    /// Demotes a success to a failure carrying an error produced by the specified factory when the carried value does
    /// not satisfy the specified predicate.
    /// </summary>
    /// <param name="predicate">The condition the carried value must satisfy.</param>
    /// <param name="errorFactory">
    /// The factory invoked with the carried value to produce the error when the predicate is not satisfied.
    /// </param>
    /// <returns>
    /// This result when it is already a failure or when a value is present and satisfies <paramref name="predicate" />;
    /// otherwise a failure carrying <c>errorFactory(value)</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="predicate" /> or <paramref name="errorFactory" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Neither the predicate nor the factory is invoked when this result is already a failure; the original error is
    /// preserved unchanged. The factory is invoked only for a present value that fails the predicate, so the error can
    /// describe the specific value that was rejected.
    /// </para>
    /// </remarks>
    public Result<T> Filter(Func<T, bool> predicate, Func<T, ResultError> errorFactory)
    {
        ThrowHelper.ThrowIfNull(predicate);
        ThrowHelper.ThrowIfNull(errorFactory);

        if (!_isSuccess || predicate(_value))
            return this;

        return CreateFailure(errorFactory(_value));
    }

    /// <summary>
    /// Transforms the error carried by a failed result using the specified selector.
    /// </summary>
    /// <param name="selector">The transformation applied to the carried error.</param>
    /// <returns>
    /// A failure carrying <c>selector(error)</c> when this result represents failure; otherwise this result unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when this result is a success.
    /// </para>
    /// </remarks>
    public Result<T> MapError(Func<ResultError, ResultError> selector)
    {
        ThrowHelper.ThrowIfNull(selector);

        return _isSuccess ? this : CreateFailure(selector(_error));
    }

    /// <summary>
    /// Invokes the specified action with the carried value when the result represents success.
    /// </summary>
    /// <param name="action">The side effect invoked with the carried value.</param>
    /// <returns>This result, unchanged, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The action is not invoked when this result is a failure; the result itself is always returned.
    /// </para>
    /// </remarks>
    public Result<T> Tap(Action<T> action)
    {
        ThrowHelper.ThrowIfNull(action);

        if (_isSuccess)
            action(_value);

        return this;
    }

    /// <summary>
    /// Invokes the specified action with the carried error when the result represents failure.
    /// </summary>
    /// <param name="action">The side effect invoked with the carried error.</param>
    /// <returns>This result, unchanged, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The action is not invoked when this result is a success; the result itself is always returned.
    /// </para>
    /// </remarks>
    public Result<T> TapError(Action<ResultError> action)
    {
        ThrowHelper.ThrowIfNull(action);

        if (!_isSuccess)
            action(_error);

        return this;
    }

    /// <summary>
    /// Collapses the result to a single value by invoking the matching branch.
    /// </summary>
    /// <typeparam name="TResult">The type produced by both branches.</typeparam>
    /// <param name="onSuccess">
    /// The projection invoked with the carried value when the result represents success.
    /// </param>
    /// <param name="onFailure">
    /// The projection invoked with the carried error when the result represents failure.
    /// </param>
    /// <returns>The value produced by the invoked branch.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var label = result.Match(v => $"got {v}", error => $"failed: {error}");
    ///]]>
    /// </code>
    /// </example>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ResultError, TResult> onFailure)
    {
        ThrowHelper.ThrowIfNull(onSuccess);
        ThrowHelper.ThrowIfNull(onFailure);

        return _isSuccess ? onSuccess(_value) : onFailure(_error);
    }

    /// <summary>
    /// Invokes the matching action for the state of the result.
    /// </summary>
    /// <param name="onSuccess">The action invoked with the carried value when the result represents success.</param>
    /// <param name="onFailure">The action invoked with the carried error when the result represents failure.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <see langword="null" />.
    /// </exception>
    public void Match(Action<T> onSuccess, Action<ResultError> onFailure)
    {
        ThrowHelper.ThrowIfNull(onSuccess);
        ThrowHelper.ThrowIfNull(onFailure);

        if (_isSuccess)
            onSuccess(_value);
        else
            onFailure(_error);
    }

    /// <summary>
    /// Returns the carried value, or the default value of <typeparamref name="T" /> when the result represents failure.
    /// </summary>
    /// <returns>The carried value, or <c>default(T)</c>.</returns>
    public T? GetValueOrDefault() =>
        _isSuccess ? _value : default;

    /// <summary>
    /// Returns the carried value, or the specified fallback when the result represents failure.
    /// </summary>
    /// <param name="defaultValue">The value returned when the result represents failure.</param>
    /// <returns>The carried value, or <paramref name="defaultValue" />.</returns>
    public T GetValueOrDefault(T defaultValue) =>
        _isSuccess ? _value : defaultValue;

    /// <summary>
    /// Returns the carried value, or the value produced by the specified factory when the result represents failure.
    /// </summary>
    /// <param name="defaultFactory">The factory invoked to produce the fallback value.</param>
    /// <returns>The carried value, or the factory's result.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="defaultFactory" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The factory is not invoked when the result represents success.
    /// </para>
    /// </remarks>
    public T GetValueOrDefault(Func<T> defaultFactory)
    {
        ThrowHelper.ThrowIfNull(defaultFactory);

        return _isSuccess ? _value : defaultFactory();
    }

    /// <summary>
    /// Returns the carried value, or throws when the result represents failure.
    /// </summary>
    /// <returns>The carried value.</returns>
    /// <exception cref="InvalidOperationException">
    /// The result represents failure. The exception message is the error's <see cref="ResultError.Message" /> when
    /// non-empty; otherwise a generic not-a-success message. The error's <see cref="ResultError.Exception" />, when
    /// present, is attached as the <see cref="Exception.InnerException" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The captured <see cref="ResultError.Exception" /> is never rethrown directly — doing so would corrupt its
    /// original stack trace. It is exposed as the inner exception of the thrown
    /// <see cref="InvalidOperationException" /> instead.
    /// </para>
    /// </remarks>
    public T GetValueOrThrow()
    {
        if (_isSuccess)
            return _value;

        var message = _error.Message.Length != 0 ? _error.Message : ResourceStrings.Op_Invalid_ResultNotSuccess;
        throw new InvalidOperationException(message, _error.Exception);
    }

    /// <summary>
    /// Converts the result to an option, discarding the error.
    /// </summary>
    /// <returns><c>Some(value)</c> when the result represents success; otherwise <c>None</c>.</returns>
    /// <remarks>
    /// <para>
    /// This is the inverse of <see cref="Option{T}.ToResult(ResultError)" />, except that the error is lost — convert
    /// back requires supplying a new error.
    /// </para>
    /// </remarks>
    public Option<T> ToOption() =>
        _isSuccess ? Option<T>.Some(_value) : Option<T>.None;
}
