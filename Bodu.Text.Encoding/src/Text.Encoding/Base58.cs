// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Base58 encoding and decoding of binary data using the Bitcoin/Flickr or Ripple alphabets.
/// </summary>
/// <remarks>
/// <para>
/// Base58 is a non-power-of-two radix (radix 58) used by Bitcoin addresses, IPFS CIDs, Solana, and similar protocols.
/// Because the radix is not a power of two, encoding is performed using big-integer arithmetic rather than the
/// bit-stream technique used by Base16, Base32, and Base64.
/// </para>
/// <para>
/// Leading zero bytes in the input are encoded as leading alphabet[0] characters (typically <c>1</c> in the
/// Bitcoin/Flickr alphabet) so that the byte-level and character-level forms preserve a meaningful prefix.
/// </para>
/// <para>
/// Base58 has no padding character and no standard decorations. The
/// <see cref="BaseFormattingOptions.UpperCase" />, <see cref="BaseFormattingOptions.IncludePrefix" />,
/// <see cref="BaseFormattingOptions.InsertSpacing" />, <see cref="BaseFormattingOptions.InsertLineBreaks" />, and
/// <see cref="BaseFormattingOptions.OmitPadding" /> flags are no-ops. The
/// <see cref="BaseFormatStyles.AllowPrefix" /> and <see cref="BaseFormatStyles.AllowMissingPadding" /> flags are
/// likewise ignored; only <see cref="BaseFormatStyles.IgnoreWhitespace" /> has an effect on decode.
/// </para>
/// </remarks>
public static partial class Base58
{

    private const string BitcoinFlickrAlphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    private const string RippleAlphabet = "rpshnaf39wBUDNEGHJKLM4PQRST7VWXYZ2bcdeCg65jkm8oFqi1tuvAxyz";

    private static readonly sbyte[] s_bitcoinFlickrLookup = BuildLookup(BitcoinFlickrAlphabet);
    private static readonly sbyte[] s_rippleLookup = BuildLookup(RippleAlphabet);

    /// <summary>
    /// Returns an upper bound on the number of bytes that decoding <paramref name="charCount" /> characters can
    /// produce.
    /// </summary>
    /// <param name="charCount">The input character count.</param>
    /// <returns>An upper bound on the decoded byte count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="charCount" /> is negative.
    /// </exception>
    public static int GetMaxDecodedLength(int charCount)
    {
        ThrowHelper.ThrowIfNegative(charCount);
        if (charCount == 0)
            return 0;
        checked
        {
            return ((charCount * 733) / 1000) + 1;
        }
    }

    /// <summary>
    /// Returns an upper bound on the number of characters required to encode <paramref name="byteCount" /> bytes.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <returns>An upper bound on the encoded character count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="byteCount" /> is negative.
    /// </exception>
    /// <remarks>
    /// Because Base58 is a non-power-of-two radix and the exact encoded length depends on the leading-zero count of
    /// the input data, this overload returns a worst-case upper bound suitable for buffer sizing. The actual output
    /// length is the result of <see cref="Encode(ReadOnlySpan{byte}, Base58Variant)" /> on the specific data.
    /// </remarks>
    public static int GetMaxEncodedLength(int byteCount)
    {
        ThrowHelper.ThrowIfNegative(byteCount);
        if (byteCount == 0)
            return 0;
        checked
        {
            return ((byteCount * 138) / 100) + 1;
        }
    }

    /// <summary>
    /// Indicates whether <paramref name="value" /> is a valid digit for the supplied Base58 variant.
    /// </summary>
    /// <param name="value">The character to test.</param>
    /// <param name="variant">The variant.</param>
    /// <returns><see langword="true" /> when the character belongs to the variant alphabet.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool IsBase58Digit(char value, Base58Variant variant = Base58Variant.BitcoinFlickr)
    {
        var lookup = GetLookup(variant);
        return value < lookup.Length && lookup[value] >= 0;
    }

    /// <summary>
    /// Indicates whether <paramref name="source" /> is a valid Base58 input under the supplied variant.
    /// </summary>
    /// <param name="source">The character span.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">Parsing styles. Only <see cref="BaseFormatStyles.IgnoreWhitespace" /> has effect.</param>
    /// <returns><see langword="true" /> when every retained character is in the variant alphabet; otherwise
    /// <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool IsValid(ReadOnlySpan<char> source, Base58Variant variant = Base58Variant.BitcoinFlickr, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        var lookup = GetLookup(variant);
        var ignoreWhitespace = styles.HasFlag(BaseFormatStyles.IgnoreWhitespace);

        foreach (var c in source)
        {
            if (ignoreWhitespace && c is ' ' or '\t' or '\r' or '\n')
                continue;

            if (c >= lookup.Length || lookup[c] < 0)
                return false;
        }

        return true;
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
    /// Returns the alphabet for the supplied variant.
    /// </summary>
    /// <param name="variant">The variant.</param>
    /// <returns>The variant alphabet.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    private static string GetAlphabet(Base58Variant variant) =>
        variant switch
        {
            Base58Variant.BitcoinFlickr => BitcoinFlickrAlphabet,
            Base58Variant.Ripple => RippleAlphabet,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown Base58 variant."),
        };

    /// <summary>
    /// Returns the lookup table for the supplied variant.
    /// </summary>
    /// <param name="variant">The variant.</param>
    /// <returns>The lookup table.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    private static sbyte[] GetLookup(Base58Variant variant) =>
        variant switch
        {
            Base58Variant.BitcoinFlickr => s_bitcoinFlickrLookup,
            Base58Variant.Ripple => s_rippleLookup,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown Base58 variant."),
        };
}
