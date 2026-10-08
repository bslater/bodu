// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedThrowHelper.CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;

using Bodu.Text.Delimited.Reader;
using Bodu.Text.Delimited.Writer;

namespace Bodu.Text.Delimited;

internal static partial class DelimitedThrowHelper
{
    /// <summary>
    /// Throws when reader options name a delimiter, quote or comment character that the reader cannot use.
    /// </summary>
    /// <param name="options">The reader options to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the effective delimiter, quote or comment character is not an ASCII character or is a carriage
    /// return or a line feed, when the delimiter equals the quote, or when the comment character equals the delimiter
    /// or the quote.
    /// </exception>
    /// <remarks>
    /// The reader matches each of these characters as a single byte of the UTF-8 input, so a character outside ASCII
    /// would be narrowed to one byte and matched inside other characters. The comment character is checked whether or
    /// not <see cref="DelimitedReaderOptions.AllowComments" /> is set, so that options stay valid when comments are
    /// turned on.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfUnusableDialect(
        DelimitedReaderOptions options,
        [CallerArgumentExpression(nameof(options))] string? paramName = null)
    {
        ThrowIfUnusableDialect(options.EffectiveDelimiter, options.EffectiveQuote, paramName);
        ThrowIfUnusableCommentChar(options.EffectiveCommentChar, options.EffectiveDelimiter, options.EffectiveQuote, paramName);
    }

    /// <summary>
    /// Throws when writer options name a delimiter or quote character that the writer cannot use.
    /// </summary>
    /// <param name="options">The writer options to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the effective delimiter or quote is not an ASCII character or is a carriage return or a line feed,
    /// or when the delimiter equals the quote.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfUnusableDialect(
        DelimitedWriterOptions options,
        [CallerArgumentExpression(nameof(options))] string? paramName = null) =>
        ThrowIfUnusableDialect(options.EffectiveDelimiter, options.EffectiveQuote, paramName);

    /// <summary>
    /// Throws when a delimiter or quote character is unusable, or when the two are the same character.
    /// </summary>
    /// <param name="delimiter">The effective delimiter.</param>
    /// <param name="quote">The effective quote character.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when either character is not ASCII or is a carriage return or a line feed, or when they are equal.
    /// </exception>
    private static void ThrowIfUnusableDialect(char delimiter, char quote, string? paramName)
    {
        ThrowIfUnusableCharacter(delimiter, nameof(DelimitedReaderOptions.Delimiter), paramName);
        ThrowIfUnusableCharacter(quote, nameof(DelimitedReaderOptions.Quote), paramName);
        ThrowIfSameCharacter(delimiter, quote, nameof(DelimitedReaderOptions.Delimiter), nameof(DelimitedReaderOptions.Quote), paramName);
    }

    /// <summary>
    /// Throws when a comment character is unusable, or when it equals the delimiter or the quote character.
    /// </summary>
    /// <param name="commentChar">The effective comment character.</param>
    /// <param name="delimiter">The effective delimiter.</param>
    /// <param name="quote">The effective quote character.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the comment character is not ASCII or is a carriage return or a line feed, or when it equals the
    /// delimiter or the quote.
    /// </exception>
    private static void ThrowIfUnusableCommentChar(char commentChar, char delimiter, char quote, string? paramName)
    {
        ThrowIfUnusableCharacter(commentChar, nameof(DelimitedReaderOptions.CommentChar), paramName);
        ThrowIfSameCharacter(commentChar, delimiter, nameof(DelimitedReaderOptions.CommentChar), nameof(DelimitedReaderOptions.Delimiter), paramName);
        ThrowIfSameCharacter(commentChar, quote, nameof(DelimitedReaderOptions.CommentChar), nameof(DelimitedReaderOptions.Quote), paramName);
    }

    /// <summary>
    /// Throws when a dialect character is not an ASCII character, or is a carriage return or a line feed.
    /// </summary>
    /// <param name="value">The effective character.</param>
    /// <param name="optionName">The name of the option that holds it.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is above U+007F, or is U+000D or U+000A.
    /// </exception>
    private static void ThrowIfUnusableCharacter(char value, string optionName, string? paramName)
    {
        if (!char.IsAscii(value) || value is '\r' or '\n')
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, DelimitedResourceStrings.Arg_Invalid_DelimitedDialectCharacter, optionName, (int)value), paramName);
    }

    /// <summary>
    /// Throws when two dialect characters are the same character.
    /// </summary>
    /// <param name="first">The first effective character.</param>
    /// <param name="second">The second effective character.</param>
    /// <param name="firstName">The name of the option that holds <paramref name="first" />.</param>
    /// <param name="secondName">The name of the option that holds <paramref name="second" />.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="first" /> equals <paramref name="second" />.
    /// </exception>
    private static void ThrowIfSameCharacter(char first, char second, string firstName, string secondName, string? paramName)
    {
        if (first == second)
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, DelimitedResourceStrings.Arg_Invalid_DelimitedDialectCharactersEqual, firstName, secondName, (int)first), paramName);
    }
}
