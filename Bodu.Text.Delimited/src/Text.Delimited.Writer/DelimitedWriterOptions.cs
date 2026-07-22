// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedWriterOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited.Writer;

/// <summary>
/// Provides configuration for a <see cref="Utf8DelimitedWriter" />, controlling the delimiter, quote character, and
/// whether a header row is emitted for object records.
/// </summary>
/// <remarks>
/// The options are an immutable value type; the delimiter and quote fall back to their RFC 4180 defaults (<c>','</c>
/// and <c>'"'</c>) when left unset.
/// </remarks>
public readonly struct DelimitedWriterOptions
{
    /// <summary>
    /// Gets the default writer options (comma delimiter, double-quote quoting, header row emitted).
    /// </summary>
    /// <value>The default options.</value>
    public static DelimitedWriterOptions Default => default;

    /// <summary>
    /// Gets the field delimiter, or <c>'\0'</c> to use the default comma.
    /// </summary>
    /// <value>The delimiter character.</value>
    public char Delimiter { get; init; }

    /// <summary>
    /// Gets the quote character, or <c>'\0'</c> to use the default double quote.
    /// </summary>
    /// <value>The quote character.</value>
    public char Quote { get; init; }

    /// <summary>
    /// Gets a value indicating whether the header row is suppressed for object records.
    /// </summary>
    /// <value><see langword="true" /> to suppress the header row; otherwise <see langword="false" />.</value>
    public bool NoHeader { get; init; }

    /// <summary>
    /// Gets the effective delimiter, resolving the unset default to a comma.
    /// </summary>
    /// <value>The delimiter character.</value>
    internal char EffectiveDelimiter => Delimiter == '\0' ? ',' : Delimiter;

    /// <summary>
    /// Gets the effective quote, resolving the unset default to a double quote.
    /// </summary>
    /// <value>The quote character.</value>
    internal char EffectiveQuote => Quote == '\0' ? '"' : Quote;
}
