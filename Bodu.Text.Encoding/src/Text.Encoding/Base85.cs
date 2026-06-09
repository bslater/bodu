// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Base85 encoding and decoding of binary data using the Adobe Ascii85 or ZeroMQ Z85 alphabets.
/// </summary>
/// <remarks>
/// <para>
/// Base85 packs four input bytes into a 32-bit unsigned integer, then divides by 85 four times to emit five output
/// characters per four-byte group — a payload expansion of 25 %, compared to Base64's 33 % and Base16's 100 %.
/// </para>
/// <para>
/// The Adobe Ascii85 variant recognises the character <c>z</c> as a shortcut for four zero bytes and permits partial
/// trailing groups of one, two, or three bytes (output grows by one character per byte plus one). The Z85 variant
/// requires the input to be a whole multiple of four bytes and does not use any shortcut.
/// </para>
/// <para>
/// Base85 has no padding character. The <see cref="BaseFormattingOptions.UpperCase" />,
/// <see cref="BaseFormattingOptions.InsertSpacing" />, <see cref="BaseFormattingOptions.InsertLineBreaks" />, and
/// <see cref="BaseFormattingOptions.OmitPadding" /> flags are ignored on the encode side.
/// <see cref="BaseFormattingOptions.IncludePrefix" /> is honoured for the <see cref="Base85Variant.Ascii85" /> variant
/// — when set, the output is wrapped in the Adobe Ascii85 <c>&lt;~</c> / <c>~&gt;</c> delimiter pair. The flag is
/// ignored for <see cref="Base85Variant.Z85" />.
/// </para>
/// <para>
/// On the decode side, <see cref="BaseFormatStyles.IgnoreWhitespace" /> permits whitespace in the input.
/// <see cref="BaseFormatStyles.AllowPrefix" /> permits an optional <c>&lt;~</c> / <c>~&gt;</c> delimiter pair around
/// the Ascii85 payload.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// byte[] data = "hello world"u8.ToArray();
///
/// // Adobe Ascii85 (default) — the form used by PostScript and PDF, with the 'z' shortcut for all-zero groups.
/// string ascii85 = Base85.Encode(data);
///
/// // Wrap the output in the Adobe <~ ... ~> delimiter pair.
/// string delimited = Base85.Encode(data, BaseFormattingOptions.IncludePrefix);
///
/// // ZeroMQ Z85 — shell-safe alphabet; the input must be a multiple of four bytes.
/// byte[] padded = "hello wor"u8.ToArray();    // 9 bytes — pad to 12 before encoding
/// string z85    = Base85.Encode(padded.AsSpan(0, 8), Base85Variant.Z85);
///
/// // Round-trip.
/// byte[] roundtrip = Base85.Decode(ascii85);
///]]>
/// </example>
public static partial class Base85
{
    private const string Ascii85Alphabet = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstu";

    /// <summary>
    /// The Adobe Ascii85 trailing delimiter emitted when <see cref="BaseFormattingOptions.IncludePrefix" /> is set.
    /// </summary>
    private const string Ascii85DelimiterEnd = "~>";

    /// <summary>
    /// The combined length of <see cref="Ascii85DelimiterStart" /> and <see cref="Ascii85DelimiterEnd" />.
    /// </summary>
    private const int Ascii85DelimiterLength = 4;

    /// <summary>
    /// The Adobe Ascii85 leading delimiter emitted when <see cref="BaseFormattingOptions.IncludePrefix" /> is set.
    /// </summary>
    private const string Ascii85DelimiterStart = "<~";
    private const string Z85Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.-:+=^!/*?&<>()[]{}@%$#";

    /// <summary>
    /// The Ascii85 <c>z</c> shortcut representing four consecutive zero bytes.
    /// </summary>
    private const char ZeroShortcut = 'z';

    private static readonly sbyte[] s_ascii85Lookup = BuildLookup(Ascii85Alphabet);
    private static readonly sbyte[] s_z85Lookup = BuildLookup(Z85Alphabet);

    /// <summary>
    /// Returns the exact number of characters that
    /// <see cref="Encode(ReadOnlySpan{byte}, Base85Variant, BaseFormattingOptions)" /> will produce for the supplied
    /// data, accounting for the Adobe Ascii85 <c>z</c> shortcut on all-zero groups and the optional Adobe Ascii85
    /// <c>&lt;~ ... ~&gt;</c> delimiter pair.
    /// </summary>
    /// <param name="source">The input bytes.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <param name="options">
    /// Formatting options. <see cref="BaseFormattingOptions.IncludePrefix" /> adds four characters for the Ascii85
    /// delimiter pair; other flags are ignored.
    /// </param>
    /// <returns>The exact encoded character count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="variant" /> is <see cref="Base85Variant.Z85" /> and the source length is not a
    /// multiple of four bytes.
    /// </exception>
    /// <remarks>
    /// For Ascii85 the exact length cannot be computed from byte count alone because four-zero groups collapse to a
    /// single <c>z</c> character. Code that needs a buffer size without scanning the data should call
    /// <see cref="GetMaxEncodedLength(int, Base85Variant, BaseFormattingOptions)" /> for the worst-case upper bound.
    /// </remarks>
    public static int GetEncodedLength(ReadOnlySpan<byte> source, Base85Variant variant = Base85Variant.Ascii85, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureValidVariant(variant);
        var emitDelimiters = ShouldEmitAscii85Delimiters(variant, options);

        if (source.IsEmpty)
            return emitDelimiters ? Ascii85DelimiterLength : 0;

        if (variant == Base85Variant.Z85)
        {
            return (source.Length & 3) != 0
                ? throw new ArgumentException(EncodingResourceStrings.Arg_Invalid_Z85InputMultipleOfFour, nameof(source))
                : (source.Length / 4) * 5;
        }

        // Ascii85: scan for the 'z' shortcut. Four consecutive zero bytes encode to a single 'z'; otherwise the
        // group is 5 chars. Trailing 1, 2, or 3 bytes encode to 2, 3, or 4 chars respectively.
        var total = 0;
        var i = 0;
        while (i + 4 <= source.Length)
        {
            if (source[i] == 0 && source[i + 1] == 0 && source[i + 2] == 0 && source[i + 3] == 0)
                total += 1;
            else
                total += 5;

            i += 4;
        }

        var remainder = source.Length - i;
        if (remainder > 0)
            total += remainder + 1;

        if (emitDelimiters)
            total += Ascii85DelimiterLength;

        return total;
    }

    /// <summary>
    /// Returns the maximum number of bytes that decoding <paramref name="charCount" /> characters could produce (worst
    /// case, assuming no <c>z</c> shortcuts expand to four zero bytes).
    /// </summary>
    /// <param name="charCount">The input character count. Must be non-negative.</param>
    /// <returns>The worst-case decoded byte count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="charCount" /> is negative.</exception>
    public static int GetMaxDecodedLength(int charCount)
    {
        ThrowHelper.ThrowIfNegative(charCount);
        if (charCount == 0)
            return 0;
        checked
        {
            return charCount * 4;
        }
    }

    /// <summary>
    /// Returns the maximum number of characters that encoding <paramref name="byteCount" /> bytes could produce (i.e.
    /// assuming no Ascii85 <c>z</c> shortcuts are emitted), with optional Adobe Ascii85 delimiters.
    /// </summary>
    /// <param name="byteCount">The input byte count. Must be non-negative.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <param name="options">
    /// Formatting options. <see cref="BaseFormattingOptions.IncludePrefix" /> adds four characters for the Ascii85
    /// delimiter pair; other flags are ignored.
    /// </param>
    /// <returns>The worst-case encoded character count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteCount" /> is negative.</exception>
    public static int GetMaxEncodedLength(int byteCount, Base85Variant variant = Base85Variant.Ascii85, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNegative(byteCount);
        EnsureValidVariant(variant);

        var emitDelimiters = ShouldEmitAscii85Delimiters(variant, options);

        if (byteCount == 0)
            return emitDelimiters ? Ascii85DelimiterLength : 0;

        var completeGroups = byteCount / 4;
        var remainder = byteCount % 4;

        checked
        {
            int total;
            if (variant == Base85Variant.Z85)
            {
                var aligned = (byteCount + 3) & ~3;
                total = (aligned / 4) * 5;
            }
            else
            {
                total = (completeGroups * 5) + (remainder == 0 ? 0 : remainder + 1);
            }

            if (emitDelimiters)
                total += Ascii85DelimiterLength;

            return total;
        }
    }

    /// <summary>
    /// Indicates whether <paramref name="value" /> is a valid digit for the supplied Base85 variant. The Ascii85
    /// <c>z</c> shortcut is not considered a digit.
    /// </summary>
    /// <param name="value">The character to test.</param>
    /// <param name="variant">The variant.</param>
    /// <returns><see langword="true" /> when the character belongs to the variant alphabet.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool IsBase85Digit(char value, Base85Variant variant = Base85Variant.Ascii85)
    {
        var lookup = GetLookup(variant);
        return value < lookup.Length && lookup[value] >= 0;
    }

    /// <summary>
    /// Indicates whether <paramref name="source" /> is a valid Base85 input under the supplied variant.
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">
    /// Parsing styles. <see cref="BaseFormatStyles.IgnoreWhitespace" /> permits whitespace in the input;
    /// <see cref="BaseFormatStyles.AllowPrefix" /> permits the optional Adobe Ascii85 <c>&lt;~</c> / <c>~&gt;</c>
    /// delimiter pair (Ascii85 only).
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every retained character is in the variant alphabet or is a recognised shortcut.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool IsValid(ReadOnlySpan<char> source, Base85Variant variant = Base85Variant.Ascii85, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        var lookup = GetLookup(variant);
        var ignoreWhitespace = styles.HasFlag(BaseFormatStyles.IgnoreWhitespace);
        var isAscii85 = variant == Base85Variant.Ascii85;

        if (isAscii85 && styles.HasFlag(BaseFormatStyles.AllowPrefix))
            source = StripAscii85Delimiters(source, ignoreWhitespace);

        // Walk the input and validate both alphabet membership AND structural rules so IsValid agrees with the
        // strict decoder. For Ascii85 the 'z' shortcut may only appear at a group boundary; trailing partial groups
        // must be 2, 3, or 4 chars (1 cannot represent any byte). Z85 requires the symbol count to be a multiple
        // of 5.
        var symbolsThisGroup = 0;
        var totalSymbols = 0;

        foreach (var c in source)
        {
            if (ignoreWhitespace && c is ' ' or '\t' or '\r' or '\n')
                continue;

            if (isAscii85 && c == ZeroShortcut)
            {
                if (symbolsThisGroup != 0)
                    return false;

                continue;
            }

            if (c >= lookup.Length || lookup[c] < 0)
                return false;

            symbolsThisGroup++;
            totalSymbols++;
            if (symbolsThisGroup == 5)
                symbolsThisGroup = 0;
        }

        if (variant == Base85Variant.Z85)
        {
            // Z85 requires the input length (excluding stripped whitespace) to be an exact multiple of five.
            return symbolsThisGroup == 0;
        }

        // Ascii85: terminal partial group of 1 character cannot produce any byte and is rejected. 0, 2, 3, 4 are
        // permitted (0 = no partial; 2/3/4 = 1/2/3 bytes of tail per the partial-group rule).
        return symbolsThisGroup != 1;
    }

    /// <summary>
    /// Builds a 128-entry lookup table mapping alphabet characters to their numeric value.
    /// </summary>
    /// <param name="alphabet">The alphabet.</param>
    /// <returns>The lookup table.</returns>
    private static sbyte[] BuildLookup(string alphabet)
    {
        var table = new sbyte[128];
        Array.Fill(table, (sbyte)-1);

        for (var i = 0; i < alphabet.Length; i++)
        {
            var c = alphabet[i];
            table[c] = (sbyte)i;
        }

        return table;
    }

    /// <summary>
    /// Validates that <paramref name="variant" /> is a defined enum value.
    /// </summary>
    /// <param name="variant">The variant to test.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    private static void EnsureValidVariant(Base85Variant variant)
    {
        if (variant is not (Base85Variant.Ascii85 or Base85Variant.Z85))
            throw new ArgumentOutOfRangeException(nameof(variant), variant, EncodingResourceStrings.Arg_OutOfRange_Base85Variant);
    }

    /// <summary>
    /// Returns the alphabet for the supplied variant.
    /// </summary>
    /// <param name="variant">The variant.</param>
    /// <returns>The variant alphabet.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    private static string GetAlphabet(Base85Variant variant) =>
        variant switch
        {
            Base85Variant.Ascii85 => Ascii85Alphabet,
            Base85Variant.Z85 => Z85Alphabet,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, EncodingResourceStrings.Arg_OutOfRange_Base85Variant),
        };

    /// <summary>
    /// Returns the lookup table for the supplied variant.
    /// </summary>
    /// <param name="variant">The variant.</param>
    /// <returns>The lookup table.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    private static sbyte[] GetLookup(Base85Variant variant) =>
        variant switch
        {
            Base85Variant.Ascii85 => s_ascii85Lookup,
            Base85Variant.Z85 => s_z85Lookup,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, EncodingResourceStrings.Arg_OutOfRange_Base85Variant),
        };

    /// <summary>
    /// Indicates whether the Adobe Ascii85 <c>&lt;~ ... ~&gt;</c> delimiter pair should be emitted given the supplied
    /// variant and options.
    /// </summary>
    /// <param name="variant">The variant.</param>
    /// <param name="options">The formatting options.</param>
    /// <returns>
    /// <see langword="true" /> when <see cref="BaseFormattingOptions.IncludePrefix" /> is set and the variant is
    /// <see cref="Base85Variant.Ascii85" />.
    /// </returns>
    private static bool ShouldEmitAscii85Delimiters(Base85Variant variant, BaseFormattingOptions options) =>
        variant == Base85Variant.Ascii85 && options.HasFlag(BaseFormattingOptions.IncludePrefix);

    /// <summary>
    /// Removes the Adobe Ascii85 <c>&lt;~</c> and <c>~&gt;</c> delimiter pair (and optional surrounding whitespace)
    /// from <paramref name="source" />.
    /// </summary>
    /// <param name="source">The input span.</param>
    /// <param name="trimSurroundingWhitespace">
    /// Whether to also strip ASCII whitespace surrounding the delimiters.
    /// </param>
    /// <returns>The input with delimiters and (optionally) surrounding whitespace removed.</returns>
    private static ReadOnlySpan<char> StripAscii85Delimiters(ReadOnlySpan<char> source, bool trimSurroundingWhitespace)
    {
        if (trimSurroundingWhitespace)
        {
            var start = 0;
            while (start < source.Length && source[start] is ' ' or '\t' or '\r' or '\n')
                start++;
            var end = source.Length;
            while (end > start && source[end - 1] is ' ' or '\t' or '\r' or '\n')
                end--;
            source = source.Slice(start, end - start);
        }

        if (source.Length >= 2 && source[0] == '<' && source[1] == '~')
            source = source.Slice(2);

        if (source.Length >= 2 && source[^1] == '>' && source[^2] == '~')
            source = source.Slice(0, source.Length - 2);

        return source;
    }
}
