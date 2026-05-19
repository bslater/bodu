// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Base32 encoding and decoding of binary data across multiple variants (RFC 4648 standard and hex-extended,
/// Crockford, z-base-32), with optional padding control and whitespace tolerance during parsing.
/// </summary>
/// <remarks>
/// <para>
/// Each variant uses 5 bits per character (radix 32) and groups input bytes into five-byte blocks that map to eight
/// output characters. The padding character <c>=</c> is appended on encode when the input is not a multiple of five
/// bytes; whether padding is emitted by default depends on the variant.
/// </para>
/// <para>
/// The <see cref="BaseFormattingOptions.UpperCase" />, <see cref="BaseFormattingOptions.IncludePrefix" />, and
/// <see cref="BaseFormattingOptions.InsertSpacing" /> flags have no effect on Base32 — each variant emits its canonical
/// alphabet case and there is no standard prefix. The <see cref="BaseFormatStyles.AllowPrefix" /> flag is also ignored
/// on decode for the same reason.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// byte[] data = "hello"u8.ToArray();
///
/// // RFC 4648 Standard Base32 (default).
/// string standard = Base32.Encode(data);                                      // "NBSWY3DP"
///
/// // Crockford Base32 — human-friendly alphabet, no padding.
/// string crockford = Base32.Encode(data, Base32Variant.Crockford);
///
/// // RFC 4648 base32hex — preserves sort order with binary keys.
/// string base32hex = Base32.Encode(data, Base32Variant.HexExtended);
///
/// // Round-trip.
/// byte[] roundtrip = Base32.Decode(standard);
///]]>
/// </example>
public static partial class Base32
{
    private const string CrockfordAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const string HexExtendedAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

    /// <summary>
    /// The number of encoded characters per output line when <see cref="BaseFormattingOptions.InsertLineBreaks" /> is
    /// requested.
    /// </summary>
    private const int LineBreakInterval = 64;

    /// <summary>
    /// The padding character used to align Base32 output to a multiple of eight characters per RFC 4648.
    /// </summary>
    private const char PaddingChar = '=';
    private const string StandardAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const string ZBase32Alphabet = "ybndrfg8ejkmcpqxot1uwisza345h769";

    private static readonly sbyte[] s_standardLookup = BuildLookup(StandardAlphabet);
    private static readonly sbyte[] s_hexExtendedLookup = BuildLookup(HexExtendedAlphabet);
    private static readonly sbyte[] s_crockfordLookup = BuildCrockfordLookup();
    private static readonly sbyte[] s_zBase32Lookup = BuildLookup(ZBase32Alphabet);

    /// <summary>
    /// Computes the exact number of bytes that decoding <paramref name="source" /> would produce after applying
    /// <paramref name="styles" /> and stripping padding for the supplied <paramref name="variant" />.
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="styles">The parsing styles.</param>
    /// <returns>The exact decoded byte count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="FormatException">
    /// Thrown when the input contains characters outside the variant alphabet, or when the digit count is invalid after
    /// stripping decorations.
    /// </exception>
    public static int GetDecodedLength(ReadOnlySpan<char> source, Base32Variant variant = Base32Variant.Standard, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        return !TryCountSymbols(source, variant, styles, out var symbolCount)
            ? throw new FormatException(EncodingResourceStrings.Format_Invalid_Base32VariantAlphabet)
            : (symbolCount * 5) / 8;
    }

    /// <summary>
    /// Returns the number of characters produced by encoding <paramref name="byteCount" /> bytes using the Standard
    /// variant with default formatting.
    /// </summary>
    /// <param name="byteCount">The input byte count. Must be non-negative.</param>
    /// <returns>The number of characters the encoder will produce.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteCount" /> is negative.</exception>
    public static int GetEncodedLength(int byteCount) =>
        GetEncodedLength(byteCount, Base32Variant.Standard, BaseFormattingOptions.None);

    /// <summary>
    /// Returns the number of characters produced by encoding <paramref name="byteCount" /> bytes using
    /// <paramref name="variant" /> with default formatting.
    /// </summary>
    /// <param name="byteCount">The input byte count. Must be non-negative.</param>
    /// <param name="variant">The Base32 variant.</param>
    /// <returns>The number of characters the encoder will produce.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="byteCount" /> is negative or <paramref name="variant" /> is undefined.
    /// </exception>
    public static int GetEncodedLength(int byteCount, Base32Variant variant) =>
        GetEncodedLength(byteCount, variant, BaseFormattingOptions.None);

    /// <summary>
    /// Computes the number of characters required to encode <paramref name="byteCount" /> bytes with the given variant
    /// and options.
    /// </summary>
    /// <param name="byteCount">The number of input bytes. Must be non-negative.</param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="options">The formatting options influencing the output length.</param>
    /// <returns>
    /// The number of characters that <see cref="Encode(ReadOnlySpan{byte}, Base32Variant, BaseFormattingOptions)" />
    /// will produce.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteCount" /> is negative.</exception>
    /// <exception cref="OverflowException">
    /// Thrown when the resulting character count would overflow <see cref="int" />.
    /// </exception>
    public static int GetEncodedLength(int byteCount, Base32Variant variant = Base32Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNegative(byteCount);

        if (byteCount == 0)
            return 0;

        var padding = ShouldEmitPadding(variant, options);
        checked
        {
            var charsBeforeLineBreaks = padding
                ? ((byteCount + 4) / 5) * 8
                : ((byteCount * 8) + 4) / 5;

            if (!options.HasFlag(BaseFormattingOptions.InsertLineBreaks) || charsBeforeLineBreaks <= LineBreakInterval)
                return charsBeforeLineBreaks;

            var breaks = (charsBeforeLineBreaks - 1) / LineBreakInterval;
            return charsBeforeLineBreaks + (breaks * 2);
        }
    }

    /// <summary>
    /// Computes the maximum number of bytes that can result from decoding <paramref name="charCount" /> characters.
    /// </summary>
    /// <param name="charCount">The number of input characters. Must be non-negative.</param>
    /// <returns>The upper bound on the decoded byte count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="charCount" /> is negative.</exception>
    public static int GetMaxDecodedLength(int charCount)
    {
        ThrowHelper.ThrowIfNegative(charCount);
        checked
        {
            return (charCount * 5) / 8;
        }
    }

    /// <summary>
    /// Indicates whether <paramref name="value" /> is a valid symbol for the supplied Base32 variant. Padding (<c>=</c>
    /// ) is not considered a symbol.
    /// </summary>
    /// <param name="value">The character to test.</param>
    /// <param name="variant">The variant.</param>
    /// <returns><see langword="true" /> when the character maps to a value within the variant's alphabet.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool IsBase32Digit(char value, Base32Variant variant = Base32Variant.Standard)
    {
        (_, var lookup) = GetVariantConfig(variant);
        return value < lookup.Length && lookup[value] >= 0;
    }

    /// <summary>
    /// Indicates whether <paramref name="source" /> is a valid Base32 input under the supplied variant and parsing
    /// styles.
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">The parsing styles.</param>
    /// <returns><see langword="true" /> when the input is valid; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool IsValid(ReadOnlySpan<char> source, Base32Variant variant = Base32Variant.Standard, BaseFormatStyles styles = BaseFormatStyles.None) =>
        TryCountSymbols(source, variant, styles, out _);

    /// <summary>
    /// Attempts to compute the exact number of decoded bytes for <paramref name="source" />.
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <param name="byteCount">
    /// When this method returns, contains the decoded byte count, or <c>0</c> on failure.
    /// </param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="styles">The parsing styles.</param>
    /// <returns>
    /// <see langword="true" /> when the input would decode cleanly; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool TryGetDecodedLength(ReadOnlySpan<char> source, out int byteCount, Base32Variant variant = Base32Variant.Standard, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        if (!TryCountSymbols(source, variant, styles, out var symbolCount))
        {
            byteCount = 0;
            return false;
        }

        byteCount = (symbolCount * 5) / 8;
        return true;
    }

    /// <summary>
    /// Builds the Crockford Base32 lookup table, including the documented aliases <c>I</c>/<c>L</c> -&gt; <c>1</c> and
    /// <c>O</c> -&gt; <c>0</c>.
    /// </summary>
    /// <returns>The Crockford lookup table.</returns>
    private static sbyte[] BuildCrockfordLookup()
    {
        var table = BuildLookup(CrockfordAlphabet);

        var one = (sbyte)CrockfordAlphabet.IndexOf('1');
        var zero = (sbyte)CrockfordAlphabet.IndexOf('0');

        table['I'] = one;
        table['i'] = one;
        table['L'] = one;
        table['l'] = one;
        table['O'] = zero;
        table['o'] = zero;

        return table;
    }

    /// <summary>
    /// Builds a 128-entry symbol lookup table from the supplied alphabet, case-folding letter characters so the decoder
    /// accepts either case.
    /// </summary>
    /// <param name="alphabet">The encoding alphabet.</param>
    /// <returns>A lookup table where valid characters map to their symbol value and others map to <c>-1</c>.</returns>
    private static sbyte[] BuildLookup(string alphabet)
    {
        var table = new sbyte[128];
        Array.Fill(table, (sbyte)-1);

        for (var i = 0; i < alphabet.Length; i++)
        {
            var c = alphabet[i];
            table[c] = (sbyte)i;

            if (char.IsLetter(c))
            {
                var lower = char.ToLowerInvariant(c);
                var upper = char.ToUpperInvariant(c);
                table[lower] = (sbyte)i;
                table[upper] = (sbyte)i;
            }
        }

        return table;
    }

    /// <summary>
    /// Returns the alphabet and the matching lookup table for the requested variant.
    /// </summary>
    /// <param name="variant">The variant to resolve.</param>
    /// <returns>A tuple containing the alphabet string and its lookup table.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="variant" /> is not a defined member of <see cref="Base32Variant" />.
    /// </exception>
    private static (string Alphabet, sbyte[] Lookup) GetVariantConfig(Base32Variant variant) =>
        variant switch
        {
            Base32Variant.Standard => (StandardAlphabet, s_standardLookup),
            Base32Variant.HexExtended => (HexExtendedAlphabet, s_hexExtendedLookup),
            Base32Variant.Crockford => (CrockfordAlphabet, s_crockfordLookup),
            Base32Variant.ZBase32 => (ZBase32Alphabet, s_zBase32Lookup),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, EncodingResourceStrings.Arg_OutOfRange_Base32Variant),
        };

    /// <summary>
    /// Determines whether the encoder should emit <c>=</c> padding for the supplied variant and options.
    /// </summary>
    /// <param name="variant">The variant in use.</param>
    /// <param name="options">The encoder options.</param>
    /// <returns><see langword="true" /> when padding should be appended; otherwise <see langword="false" />.</returns>
    private static bool ShouldEmitPadding(Base32Variant variant, BaseFormattingOptions options) =>
     !options.HasFlag(BaseFormattingOptions.OmitPadding) && variant switch
     {
         Base32Variant.Standard => true,
         Base32Variant.HexExtended => true,
         Base32Variant.Crockford => false,
         Base32Variant.ZBase32 => false,
         _ => false,
     };

    /// <summary>
    /// Walks <paramref name="source" />, validating each non-decoration character against the variant alphabet, and
    /// reports the number of data symbols (excluding padding).
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">The parsing styles.</param>
    /// <param name="symbolCount">When this method returns, contains the number of data symbols.</param>
    /// <returns><see langword="true" /> when every character is valid; otherwise <see langword="false" />.</returns>
    private static bool TryCountSymbols(ReadOnlySpan<char> source, Base32Variant variant, BaseFormatStyles styles, out int symbolCount)
    {
        (_, var lookup) = GetVariantConfig(variant);
        var ignoreWhitespace = styles.HasFlag(BaseFormatStyles.IgnoreWhitespace);

        symbolCount = 0;
        var paddingSeen = 0;

        foreach (var c in source)
        {
            if (ignoreWhitespace && c is ' ' or '\t' or '\r' or '\n')
                continue;

            if (c == PaddingChar)
            {
                paddingSeen++;
                continue;
            }

            if (paddingSeen > 0)
            {
                symbolCount = 0;
                return false;
            }

            if (c >= lookup.Length || lookup[c] < 0)
            {
                symbolCount = 0;
                return false;
            }

            symbolCount++;
        }

        // Terminal-quantum data character count must be one of {0, 2, 4, 5, 7, 8} per RFC 4648 §6. Crockford and
        // Z-Base32 do not impose a fixed terminal-quantum rule (the spec defines five bits per character with no
        // alignment restriction) so the check is gated by variant.
        var strictQuantum = variant is Base32Variant.Standard or Base32Variant.HexExtended;
        var dataMod = symbolCount % 8;
        if (strictQuantum && dataMod is 1 or 3 or 6)
        {
            symbolCount = 0;
            return false;
        }

        // Padding alignment: Standard / HexExtended require canonical padding by default. AllowMissingPadding
        // bypasses the "must have padding" requirement; Crockford / Z-Base32 ignore padding entirely. In every
        // case, if padding IS present it must match the canonical count for the data — partial or excessive
        // padding ("MZXW6Y========" or "M=") is always rejected.
        if (variant is Base32Variant.Standard or Base32Variant.HexExtended)
        {
            var expectedPadding = (8 - dataMod) % 8;
            var padIsRequired = !styles.HasFlag(BaseFormatStyles.AllowMissingPadding);

            if (padIsRequired)
            {
                var totalSymbols = symbolCount + paddingSeen;
                if ((totalSymbols % 8) != 0)
                {
                    symbolCount = 0;
                    return false;
                }

                if (paddingSeen != expectedPadding)
                {
                    symbolCount = 0;
                    return false;
                }
            }
            else if (paddingSeen != 0 && paddingSeen != expectedPadding)
            {
                // AllowMissingPadding mode: padding may be absent OR canonical, but never partial / excessive.
                symbolCount = 0;
                return false;
            }
        }
        else if (paddingSeen > 0)
        {
            // Crockford and Z-Base32 do not use padding at all. Any padding character is a structural error.
            symbolCount = 0;
            return false;
        }

        return true;
    }
}
