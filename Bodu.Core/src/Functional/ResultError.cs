// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultError.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Functional;

/// <summary>
/// Represents the error carried by a failed <see cref="Result" /> or <see cref="Result{T}" />: a human-readable
/// message, an optional machine-readable code, and an optional originating exception.
/// </summary>
/// <remarks>
/// <para>
/// Create errors with <see cref="FromMessage(string, string?)" /> or <see cref="FromException(Exception, string?)" />,
/// or lift a plain message through the implicit conversion from <see cref="string" />. <c>default(ResultError)</c> is
/// valid and total: <see cref="Message" /> returns <see cref="string.Empty" /> while <see cref="Code" /> and
/// <see cref="Exception" /> are <see langword="null" />.
/// </para>
/// <para>
/// Equality compares <see cref="Code" /> and <see cref="Message" /> ordinally and <see cref="Exception" /> by
/// reference; the hash code is computed from <see cref="Code" /> and <see cref="Message" /> only, so two errors that
/// differ solely by exception instance hash identically but compare unequal.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct ResultError
    : System.IEquatable<ResultError>
{
    /// <summary>The optional machine-readable code, or <see langword="null" /> when none was supplied.</summary>
    private readonly string? _code;

    /// <summary>The message backing field; <see langword="null" /> only for <c>default(ResultError)</c>.</summary>
    private readonly string? _message;

    /// <summary>The originating exception, or <see langword="null" /> when none was captured.</summary>
    private readonly Exception? _exception;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultError" /> struct with the specified components.
    /// </summary>
    /// <param name="code">The optional machine-readable code.</param>
    /// <param name="message">
    /// The message to store. The caller has already validated it is not <see langword="null" />.
    /// </param>
    /// <param name="exception">The optional originating exception.</param>
    private ResultError(string? code, string message, Exception? exception)
    {
        _code = code;
        _message = message;
        _exception = exception;
    }

    /// <summary>
    /// Gets the optional machine-readable error code.
    /// </summary>
    /// <value>The code supplied when the error was created, or <see langword="null" /> when none was supplied.</value>
    public string? Code =>
        _code;

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    /// <value>
    /// The message supplied when the error was created. Never <see langword="null" /> — <see cref="string.Empty" /> for
    /// <c>default(ResultError)</c>.
    /// </value>
    public string Message =>
        _message ?? string.Empty;

    /// <summary>
    /// Gets the exception the error was created from, when any.
    /// </summary>
    /// <value>
    /// The exception passed to <see cref="FromException(Exception, string?)" />, or <see langword="null" /> when the
    /// error did not originate from an exception.
    /// </value>
    public Exception? Exception =>
        _exception;

    /// <summary>
    /// Creates an error from the specified message and optional code.
    /// </summary>
    /// <param name="message">The human-readable error message. Must not be <see langword="null" />.</param>
    /// <param name="code">The optional machine-readable code.</param>
    /// <returns>A <see cref="ResultError" /> carrying the message and code, with no exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> is <see langword="null" />.</exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var plain = ResultError.FromMessage("The file was not found.");
    /// var coded = ResultError.FromMessage("The file was not found.", "IO.NotFound");
    ///]]>
    /// </code>
    /// </example>
    public static ResultError FromMessage(string message, string? code = null)
    {
        ThrowHelper.ThrowIfNull(message);

        return new ResultError(code, message, null);
    }

    /// <summary>
    /// Creates an error from the specified exception and optional code.
    /// </summary>
    /// <param name="exception">The originating exception. Must not be <see langword="null" />.</param>
    /// <param name="code">The optional machine-readable code.</param>
    /// <returns>
    /// A <see cref="ResultError" /> whose <see cref="Message" /> is the exception's <see cref="Exception.Message" />
    /// and whose <see cref="Exception" /> is the exception itself.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception" /> is <see langword="null" />.</exception>
    public static ResultError FromException(Exception exception, string? code = null)
    {
        ThrowHelper.ThrowIfNull(exception);

        return new ResultError(code, exception.Message, exception);
    }

    /// <summary>
    /// Converts a message string to an error with no code and no exception.
    /// </summary>
    /// <param name="message">The human-readable error message. Must not be <see langword="null" />.</param>
    /// <returns>A <see cref="ResultError" /> carrying the message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The conversion delegates to <see cref="FromMessage(string, string?)" /> and therefore rejects a
    /// <see langword="null" /> string rather than producing a default error.
    /// </para>
    /// </remarks>
    public static implicit operator ResultError(string message) =>
        FromMessage(message);

    /// <summary>
    /// Determines whether two errors are equal.
    /// </summary>
    /// <param name="left">The first error to compare.</param>
    /// <param name="right">The second error to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the codes and messages compare equal ordinally and the exceptions are the same
    /// instance; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator ==(ResultError left, ResultError right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two errors are not equal.
    /// </summary>
    /// <param name="left">The first error to compare.</param>
    /// <param name="right">The second error to compare.</param>
    /// <returns><see langword="true" /> if the errors differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(ResultError left, ResultError right) =>
        !left.Equals(right);

    /// <summary>
    /// Determines whether the specified object is equal to the current error.
    /// </summary>
    /// <param name="obj">
    /// The object to compare. Only another <see cref="ResultError" /> can be equal; all other types, including
    /// <see langword="null" />, return <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is a <see cref="ResultError" /> equal to this instance;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is ResultError other && Equals(other);

    /// <summary>
    /// Determines whether the specified error is equal to the current instance.
    /// </summary>
    /// <param name="other">The error to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if <see cref="Code" /> and <see cref="Message" /> compare equal ordinally and
    /// <see cref="Exception" /> is the same instance; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(ResultError other) =>
        string.Equals(_code, other._code, StringComparison.Ordinal)
            && string.Equals(Message, other.Message, StringComparison.Ordinal)
            && ReferenceEquals(_exception, other._exception);

    /// <summary>
    /// Returns a hash code for the current instance consistent with <see cref="Equals(ResultError)" />.
    /// </summary>
    /// <returns>
    /// A hash code combining <see cref="Code" /> and <see cref="Message" />. <see cref="Exception" /> is deliberately
    /// excluded so errors differing only by exception instance still hash identically.
    /// </returns>
    public override int GetHashCode() =>
        HashCode.Combine(_code, Message);

    /// <summary>
    /// Returns a string representation of the error.
    /// </summary>
    /// <returns>
    /// <see cref="Message" /> when <see cref="Code" /> is <see langword="null" /> or empty; otherwise
    /// <c>"Code: Message"</c>.
    /// </returns>
    public override string ToString() =>
        string.IsNullOrEmpty(_code) ? Message : $"{_code}: {Message}";
}
