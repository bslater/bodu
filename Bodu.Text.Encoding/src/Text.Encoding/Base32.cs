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
/// <see cref="BaseFormattingOptions.InsertSpacing" /> flags have no effect on Base32 — each variant emits its
/// canonical alphabet case and there is no standard prefix. The <see cref="BaseFormatStyles.AllowPrefix" /> flag is
/// also ignored on decode for the same reason.
/// </para>
/// </remarks>
public static partial class Base32
{
    private const string StandardAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const string HexExtendedAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUV";
    private const string CrockfordAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const string ZBase32Alphabet = "ybndrfg8ejkmcpqxot1uwisza345h769";

    /// <summary>
    /// The padding character used to align Base32 output to a multiple of eight characters per RFC 4648.
    /// </summary>
    private const char PaddingChar = '=';

    /// <summary>
    /// The number of encoded characters per output line when
    /// <see cref="BaseFormattingOptions.InsertLineBreaks" /> is requested.
    /// </summary>
    private const int LineBreakInterval = 64;

    private static readonly sbyte[] s_standardLookup = BuildLookup(StandardAlphabet);
    private static readonly sbyte[] s_hexExtendedLookup = BuildLookup(HexExtendedAlphabet);
    private static readonly sbyte[] s_crockfordLookup = BuildCrockfordLookup();
    private static readonly sbyte[] s_zBase32Lookup = BuildLookup(ZBase32Alphabet);

    /// <summary>
    /// Computes the number of characters required to encode <paramref name="byteCount" /> bytes with the given
    /// variant and options.
    /// </summary>
    /// <param name="byteCount">The number of input bytes. Must be non-negative.</param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="options">The formatting options influencing the output length.</param>
    /// <returns>The number of characters that <see cref="Encode(ReadOnlySpan{byte}, Base32Variant, BaseFormattingOptions)" />
    /// will produce.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="byteCount" /> is negative.
    /// </exception>
    public static int GetEncodedLength(int byteCount, Base32Variant variant = Base32Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNegative(byteCount);

        if (byteCount == 0)
            return 0;

        bool padding = ShouldEmitPadding(variant, options);
        int charsBeforeLineBreaks = padding
            ? ((byteCount + 4) / 5) * 8
            : ((byteCount * 8) + 4) / 5;

        if (!options.HasFlag(BaseFormattingOptions.InsertLineBreaks) || charsBeforeLineBreaks <= LineBreakInterval)
            return charsBeforeLineBreaks;

        int breaks = (charsBeforeLineBreaks - 1) / LineBreakInterval;
        return charsBeforeLineBreaks + (breaks * 2);
    }

    /// <summary>
    /// Computes the maximum number of bytes that can result from decoding <paramref name="charCount" /> characters.
    /// </summary>
    /// <param name="charCount">The number of input characters. Must be non-negative.</param>
    /// <returns>The upper bound on the decoded byte count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="charCount" /> is negative.
    /// </exception>
    public static int GetMaxDecodedLength(int charCount)
    {
        ThrowHelper.ThrowIfNegative(charCount);
        return (charCount * 5) / 8;
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
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown Base32 variant."),
        };

    /// <summary>
    /// Determines whether the encoder should emit <c>=</c> padding for the supplied variant and options.
    /// </summary>
    /// <param name="variant">The variant in use.</param>
    /// <param name="options">The encoder options.</param>
    /// <returns><see langword="true" /> when padding should be appended; otherwise <see langword="false" />.</returns>
    private static bool ShouldEmitPadding(Base32Variant variant, BaseFormattingOptions options)
    {
        if (options.HasFlag(BaseFormattingOptions.OmitPadding))
            return false;

        return variant switch
        {
            Base32Variant.Standard => true,
            Base32Variant.HexExtended => true,
            Base32Variant.Crockford => false,
            Base32Variant.ZBase32 => false,
            _ => false,
        };
    }

    /// <summary>
    /// Builds a 128-entry symbol lookup table from the supplied alphabet, case-folding letter characters so the
    /// decoder accepts either case.
    /// </summary>
    /// <param name="alphabet">The encoding alphabet.</param>
    /// <returns>A lookup table where valid characters map to their symbol value and others map to <c>-1</c>.</returns>
    private static sbyte[] BuildLookup(string alphabet)
    {
        sbyte[] table = new sbyte[128];
        Array.Fill(table, (sbyte)-1);

        for (int i = 0; i < alphabet.Length; i++)
        {
            char c = alphabet[i];
            table[c] = (sbyte)i;

            if (char.IsLetter(c))
            {
                char lower = char.ToLowerInvariant(c);
                char upper = char.ToUpperInvariant(c);
                table[lower] = (sbyte)i;
                table[upper] = (sbyte)i;
            }
        }

        return table;
    }

    /// <summary>
    /// Builds the Crockford Base32 lookup table, including the documented aliases <c>I</c>/<c>L</c> -&gt; <c>1</c>
    /// and <c>O</c> -&gt; <c>0</c>.
    /// </summary>
    /// <returns>The Crockford lookup table.</returns>
    private static sbyte[] BuildCrockfordLookup()
    {
        sbyte[] table = BuildLookup(CrockfordAlphabet);

        sbyte one = (sbyte)CrockfordAlphabet.IndexOf('1');
        sbyte zero = (sbyte)CrockfordAlphabet.IndexOf('0');

        table['I'] = one;
        table['i'] = one;
        table['L'] = one;
        table['l'] = one;
        table['O'] = zero;
        table['o'] = zero;

        return table;
    }
}
