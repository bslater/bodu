// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Base16 (hexadecimal) encoding and decoding of binary data, with support for flexible output formatting
/// (case selection, byte spacing, line wrapping, <c>0x</c> prefix) and lenient input parsing (prefix tolerance,
/// whitespace stripping).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Encode(ReadOnlySpan{byte}, BaseFormattingOptions)" /> emits two hexadecimal characters per input byte
/// using the configured <see cref="BaseFormattingOptions" /> flags.
/// <see cref="Decode(ReadOnlySpan{char}, BaseFormatStyles)" /> reverses the operation and accepts decoration tolerance
/// via <see cref="BaseFormatStyles" />.
/// </para>
/// <para>
/// All public methods are thread-safe — the type is stateless. The implementation provides allocation-minimal fast
/// paths when no decoration flags are set, and falls back to a <see cref="System.Text.StringBuilder" />-based writer
/// when spacing, prefix, or line breaks are requested.
/// </para>
/// </remarks>
public static partial class Base16
{
    /// <summary>
    /// The Base16 alphabet using lower case letters.
    /// </summary>
    private const string HexLowerAlphabet = "0123456789abcdef";

    /// <summary>
    /// The Base16 alphabet using upper case letters.
    /// </summary>
    private const string HexUpperAlphabet = "0123456789ABCDEF";

    /// <summary>
    /// The number of encoded hexadecimal characters per output line when
    /// <see cref="BaseFormattingOptions.InsertLineBreaks" /> is requested.
    /// </summary>
    private const int LineBreakInterval = 64;

    /// <summary>
    /// The decorative prefix emitted when <see cref="BaseFormattingOptions.IncludePrefix" /> is requested, and the
    /// prefix accepted when <see cref="BaseFormatStyles.AllowPrefix" /> is set.
    /// </summary>
    private const string Prefix = "0x";

    /// <summary>
    /// Computes the number of characters required to encode <paramref name="byteCount" /> bytes with the supplied
    /// formatting options.
    /// </summary>
    /// <param name="byteCount">The number of input bytes. Must be non-negative.</param>
    /// <param name="options">The formatting options that influence the output length.</param>
    /// <returns>The exact number of characters that <see cref="Encode(ReadOnlySpan{byte}, BaseFormattingOptions)" />
    /// will produce.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="byteCount" /> is negative.
    /// </exception>
    public static int GetEncodedLength(int byteCount, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNegative(byteCount);

        if (byteCount == 0)
            return options.HasFlag(BaseFormattingOptions.IncludePrefix) ? Prefix.Length : 0;

        bool spacing = options.HasFlag(BaseFormattingOptions.InsertSpacing);
        bool lineBreaks = options.HasFlag(BaseFormattingOptions.InsertLineBreaks);
        bool prefix = options.HasFlag(BaseFormattingOptions.IncludePrefix);

        int chars = spacing ? (byteCount * 3) - 1 : byteCount * 2;
        if (prefix)
            chars += Prefix.Length;

        if (lineBreaks)
        {
            int encodedCharsInLine = prefix ? Prefix.Length : 0;
            int breaks = 0;
            int remaining = chars - (prefix ? Prefix.Length : 0);

            // Tally how many \r\n insertions the encoder will emit. The encoder writes the break before emitting a
            // byte once the running column count reaches the interval, so the count is floor((written - first byte
            // already in line) / interval) — see EncodeWithFormatting for the matching loop.
            int column = encodedCharsInLine;
            for (int i = 0; i < byteCount; i++)
            {
                if (spacing && i > 0)
                    column++;

                if (column >= LineBreakInterval)
                {
                    breaks++;
                    column = 0;
                }

                column += 2;
            }

            chars += breaks * 2; // "\r\n" per break
        }

        return chars;
    }

    /// <summary>
    /// Computes the maximum number of bytes that can result from decoding <paramref name="charCount" /> characters.
    /// </summary>
    /// <param name="charCount">The number of input characters. Must be non-negative.</param>
    /// <returns>The upper bound on the number of decoded bytes. The actual byte count will be lower when the input
    /// contains decorations that are stripped during parsing.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="charCount" /> is negative.
    /// </exception>
    public static int GetMaxDecodedLength(int charCount)
    {
        ThrowHelper.ThrowIfNegative(charCount);
        return charCount / 2;
    }
}
