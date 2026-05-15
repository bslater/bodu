// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
/// Base85 has no padding character. The
/// <see cref="BaseFormattingOptions.UpperCase" />, <see cref="BaseFormattingOptions.IncludePrefix" />,
/// <see cref="BaseFormattingOptions.InsertSpacing" />, <see cref="BaseFormattingOptions.InsertLineBreaks" />, and
/// <see cref="BaseFormattingOptions.OmitPadding" /> flags are ignored on the encode side. The decoder honours
/// <see cref="BaseFormatStyles.IgnoreWhitespace" /> only.
/// </para>
/// </remarks>
public static partial class Base85
{
    private const string Ascii85Alphabet = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstu";
    private const string Z85Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.-:+=^!/*?&<>()[]{}@%$#";

    /// <summary>
    /// The Ascii85 <c>z</c> shortcut representing four consecutive zero bytes.
    /// </summary>
    private const char ZeroShortcut = 'z';

    private static readonly sbyte[] s_ascii85Lookup = BuildLookup(Ascii85Alphabet);
    private static readonly sbyte[] s_z85Lookup = BuildLookup(Z85Alphabet);

    /// <summary>
    /// Returns the exact number of characters that <see cref="Encode(ReadOnlySpan{byte}, Base85Variant)" /> will
    /// produce for the supplied data, accounting for the Adobe Ascii85 <c>z</c> shortcut on all-zero groups.
    /// </summary>
    /// <param name="source">The input bytes.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <returns>The exact encoded character count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="variant" /> is <see cref="Base85Variant.Z85" /> and the source length is not a
    /// multiple of four bytes.
    /// </exception>
    /// <remarks>
    /// For Ascii85 the exact length cannot be computed from byte count alone because four-zero groups collapse to a
    /// single <c>z</c> character. Code that needs a buffer size without scanning the data should call
    /// <see cref="GetMaxEncodedLength(int, Base85Variant)" /> for the worst-case upper bound.
    /// </remarks>
    public static int GetEncodedLength(ReadOnlySpan<byte> source, Base85Variant variant = Base85Variant.Ascii85)
    {
        EnsureValidVariant(variant);

        if (source.IsEmpty)
            return 0;

        if (variant == Base85Variant.Z85)
        {
            if ((source.Length & 3) != 0)
                throw new ArgumentException("Z85 input length must be a multiple of four bytes.", nameof(source));

            return (source.Length / 4) * 5;
        }

        // Ascii85: scan for the 'z' shortcut. Four consecutive zero bytes encode to a single 'z'; otherwise the
        // group is 5 chars. Trailing 1, 2, or 3 bytes encode to 2, 3, or 4 chars respectively.
        int total = 0;
        int i = 0;
        while (i + 4 <= source.Length)
        {
            if (source[i] == 0 && source[i + 1] == 0 && source[i + 2] == 0 && source[i + 3] == 0)
                total += 1;
            else
                total += 5;

            i += 4;
        }

        int remainder = source.Length - i;
        if (remainder > 0)
            total += remainder + 1;

        return total;
    }

    /// <summary>
    /// Returns the maximum number of characters that encoding <paramref name="byteCount" /> bytes could produce
    /// (i.e. assuming no Ascii85 <c>z</c> shortcuts are emitted).
    /// </summary>
    /// <param name="byteCount">The input byte count. Must be non-negative.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <returns>The worst-case encoded character count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteCount" /> is negative.</exception>
    public static int GetMaxEncodedLength(int byteCount, Base85Variant variant = Base85Variant.Ascii85)
    {
        ThrowHelper.ThrowIfNegative(byteCount);
        EnsureValidVariant(variant);

        if (byteCount == 0)
            return 0;

        int completeGroups = byteCount / 4;
        int remainder = byteCount % 4;

        checked
        {
            if (variant == Base85Variant.Z85)
            {
                int aligned = (byteCount + 3) & ~3;
                return (aligned / 4) * 5;
            }

            return (completeGroups * 5) + (remainder == 0 ? 0 : remainder + 1);
        }
    }

    /// <summary>
    /// Returns the maximum number of bytes that decoding <paramref name="charCount" /> characters could produce
    /// (worst case, assuming no <c>z</c> shortcuts expand to four zero bytes).
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
    /// Indicates whether <paramref name="source" /> is a valid Base85 input under the supplied variant.
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">Parsing styles. Only <see cref="BaseFormatStyles.IgnoreWhitespace" /> has effect.</param>
    /// <returns><see langword="true" /> when every retained character is in the variant alphabet or is a recognised
    /// shortcut.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool IsValid(ReadOnlySpan<char> source, Base85Variant variant = Base85Variant.Ascii85, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        sbyte[] lookup = GetLookup(variant);
        bool ignoreWhitespace = styles.HasFlag(BaseFormatStyles.IgnoreWhitespace);
        bool isAscii85 = variant == Base85Variant.Ascii85;

        // Walk the input and validate both alphabet membership AND structural rules so IsValid agrees with the
        // strict decoder. For Ascii85 the 'z' shortcut may only appear at a group boundary; trailing partial groups
        // must be 2, 3, or 4 chars (1 cannot represent any byte). Z85 requires the symbol count to be a multiple
        // of 5.
        int symbolsThisGroup = 0;
        int totalSymbols = 0;

        foreach (char c in source)
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
    /// Indicates whether <paramref name="value" /> is a valid digit for the supplied Base85 variant. The Ascii85
    /// <c>z</c> shortcut is not considered a digit.
    /// </summary>
    /// <param name="value">The character to test.</param>
    /// <param name="variant">The variant.</param>
    /// <returns><see langword="true" /> when the character belongs to the variant alphabet.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool IsBase85Digit(char value, Base85Variant variant = Base85Variant.Ascii85)
    {
        sbyte[] lookup = GetLookup(variant);
        return value < lookup.Length && lookup[value] >= 0;
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
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown Base85 variant."),
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
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown Base85 variant."),
        };

    /// <summary>
    /// Validates that <paramref name="variant" /> is a defined enum value.
    /// </summary>
    /// <param name="variant">The variant to test.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    private static void EnsureValidVariant(Base85Variant variant)
    {
        if (variant is not (Base85Variant.Ascii85 or Base85Variant.Z85))
            throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown Base85 variant.");
    }

    /// <summary>
    /// Builds a 128-entry lookup table mapping alphabet characters to their numeric value.
    /// </summary>
    /// <param name="alphabet">The alphabet.</param>
    /// <returns>The lookup table.</returns>
    private static sbyte[] BuildLookup(string alphabet)
    {
        sbyte[] table = new sbyte[128];
        Array.Fill(table, (sbyte)-1);

        for (int i = 0; i < alphabet.Length; i++)
        {
            char c = alphabet[i];
            table[c] = (sbyte)i;
        }

        return table;
    }
}
