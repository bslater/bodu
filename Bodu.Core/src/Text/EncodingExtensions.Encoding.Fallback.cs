// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.Encoding.Fallback.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

public static partial class EncodingExtensions
{
    /// <summary>
    /// Returns a clone of <paramref name="encoding" /> whose encoder and decoder fallbacks both throw
    /// <see cref="System.Text.EncoderFallbackException" /> / <see cref="System.Text.DecoderFallbackException" /> on
    /// invalid sequences.
    /// </summary>
    /// <example>
    ///<![CDATA[
    /// // Configure a single call-site to fail fast on bad input without mutating the global default.
    /// System.Text.Encoding strictAscii = System.Text.Encoding.ASCII.WithExceptionFallbacks();
    /// try
    /// {
    ///     byte[] bytes = strictAscii.GetBytes(userSuppliedText);
    /// }
    /// catch (System.Text.EncoderFallbackException ex)
    /// {
    ///     // Surface the offending character at ex.Index for diagnostics.
    /// }
    ///]]>
    /// </example>
    /// <param name="encoding">The source encoding to clone.</param>
    /// <returns>
    /// A new <see cref="System.Text.Encoding" /> instance configured with
    /// <see cref="System.Text.EncoderExceptionFallback" /> and <see cref="System.Text.DecoderExceptionFallback" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The returned encoding is a fresh instance — singleton encodings such as <see cref="System.Text.Encoding.UTF8" />
    /// are not mutated. Use this method to opt a specific call-site into strict validation without altering the global
    /// default.
    /// </remarks>
    public static System.Text.Encoding WithExceptionFallbacks(this System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return System.Text.Encoding.GetEncoding(
            encoding.CodePage,
            System.Text.EncoderFallback.ExceptionFallback,
            System.Text.DecoderFallback.ExceptionFallback);
    }

    /// <summary>
    /// Returns a clone of <paramref name="encoding" /> whose encoder fallback substitutes
    /// <paramref name="encoderReplacement" /> and whose decoder fallback substitutes
    /// <paramref name="decoderReplacement" /> for invalid sequences.
    /// </summary>
    /// <param name="encoding">The source encoding to clone.</param>
    /// <param name="encoderReplacement">
    /// The replacement string used when an encoder cannot represent a character. Defaults to <c>"?"</c>, which every
    /// supported encoding can represent.
    /// </param>
    /// <param name="decoderReplacement">
    /// The replacement string used when a decoder cannot interpret a sequence. Defaults to <c>"?"</c> rather than the
    /// Unicode replacement character (<c>U+FFFD</c>) because non-Unicode encodings — including ASCII — cannot represent
    /// <c>U+FFFD</c>.
    /// </param>
    /// <returns>
    /// A new <see cref="System.Text.Encoding" /> instance configured with
    /// <see cref="System.Text.EncoderReplacementFallback" /> and <see cref="System.Text.DecoderReplacementFallback" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />, or when either replacement is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="encoderReplacement" /> cannot be encoded by <paramref name="encoding" />. The
    /// exception's <see cref="Exception.InnerException" /> is the <see cref="System.Text.EncoderFallbackException" />
    /// reported by a strict-fallback clone of <paramref name="encoding" /> when asked to encode the replacement.
    /// </exception>
    public static System.Text.Encoding WithReplacementFallbacks(
        this System.Text.Encoding encoding,
        string encoderReplacement = "?",
        string decoderReplacement = "?")
    {
        ThrowHelper.ThrowIfNull(encoding);
        ThrowHelper.ThrowIfNull(encoderReplacement);
        ThrowHelper.ThrowIfNull(decoderReplacement);

        EnsureReplacementIsEncodable(encoding, encoderReplacement, nameof(encoderReplacement));

        return System.Text.Encoding.GetEncoding(
            encoding.CodePage,
            new System.Text.EncoderReplacementFallback(encoderReplacement),
            new System.Text.DecoderReplacementFallback(decoderReplacement));
    }

    /// <summary>
    /// Verifies that <paramref name="replacement" /> can be encoded by <paramref name="encoding" /> by routing through
    /// a clone configured with <see cref="System.Text.EncoderExceptionFallback" /> and re-throwing as
    /// <see cref="ArgumentException" /> when the encoder rejects the replacement.
    /// </summary>
    /// <param name="encoding">The encoding that will receive the replacement.</param>
    /// <param name="replacement">The replacement string to validate.</param>
    /// <param name="paramName">The parameter name reported in the resulting exception.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="encoding" /> cannot encode <paramref name="replacement" />.
    /// </exception>
    private static void EnsureReplacementIsEncodable(
        System.Text.Encoding encoding,
        string replacement,
        string paramName)
    {
        if (replacement.Length == 0) return;

        var strict = System.Text.Encoding.GetEncoding(
            encoding.CodePage,
            System.Text.EncoderFallback.ExceptionFallback,
            System.Text.DecoderFallback.ExceptionFallback);
        try
        {
            _ = strict.GetByteCount(replacement);
        }
        catch (System.Text.EncoderFallbackException ex)
        {
            throw new ArgumentException(
                string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    ResourceStrings.Arg_Invalid_ReplacementNotEncodable,
                    replacement),
                paramName,
                ex);
        }
    }

    /// <summary>
    /// Returns a value indicating whether <paramref name="encoding" /> uses
    /// <see cref="System.Text.EncoderExceptionFallback" /> and <see cref="System.Text.DecoderExceptionFallback" />.
    /// </summary>
    /// <param name="encoding">The encoding to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when both the encoder and decoder fallbacks are the BCL exception fallbacks; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Only the built-in <see cref="System.Text.EncoderExceptionFallback" /> /
    /// <see cref="System.Text.DecoderExceptionFallback" /> types are recognised. Custom fallback subclasses return
    /// <see langword="false" />.
    /// </remarks>
    public static bool UsesExceptionFallbacks(this System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.EncoderFallback is System.Text.EncoderExceptionFallback
            && encoding.DecoderFallback is System.Text.DecoderExceptionFallback;
    }

    /// <summary>
    /// Returns a value indicating whether <paramref name="encoding" /> uses
    /// <see cref="System.Text.EncoderReplacementFallback" /> and <see cref="System.Text.DecoderReplacementFallback" />.
    /// </summary>
    /// <param name="encoding">The encoding to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when both the encoder and decoder fallbacks are the BCL replacement fallbacks; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Only the built-in <see cref="System.Text.EncoderReplacementFallback" /> /
    /// <see cref="System.Text.DecoderReplacementFallback" /> types are recognised. Custom fallback subclasses return
    /// <see langword="false" />.
    /// </remarks>
    public static bool UsesReplacementFallbacks(this System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.EncoderFallback is System.Text.EncoderReplacementFallback
            && encoding.DecoderFallback is System.Text.DecoderReplacementFallback;
    }
}
