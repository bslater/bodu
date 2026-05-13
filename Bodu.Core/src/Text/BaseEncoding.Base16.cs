// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BaseEncoding.Base16.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Text;               // for StringBuilder fallback
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;

namespace Bodu.Text;

// ─────────────────────────────────────────────────────────────────────────────── Base16 implementation ───────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Partial <see cref="BaseEncoding"/> class – Base16 (hexadecimal) helpers.
/// </summary>
public static partial class BaseEncoding
{
	private const string HexLowerAlphabet = "0123456789abcdef";
	private const string HexUpperAlphabet = "0123456789ABCDEF";

	/// <summary>
	/// Decodes a portion of a character array containing hexadecimal-encoded data into a byte array.
	/// </summary>
	/// <param name="inArray">The character array containing Base16 (hex) encoded characters.</param>
	/// <param name="offset">The zero-based starting position within <paramref name="inArray" />.</param>
	/// <param name="length">The number of characters to decode, which must be an even number.</param>
	/// <param name="options">Formatting options that allow optional prefix and whitespace to be ignored during parsing.</param>
	/// <returns>A byte array representing the decoded binary data.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="inArray" /> is <c>null</c>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown if <paramref name="offset" /> or <paramref name="length" /> is invalid for the given array.
	/// </exception>
	/// <exception cref="FormatException">Thrown if the input is not valid Base16 format.</exception>
	/// <remarks>This method supports lenient parsing with flags such as <see cref="BaseFormatStyles.AllowPrefix" /> and <see cref="BaseFormatStyles.IgnoreWhitespace" />.</remarks>
	public static byte[] FromBase16CharArray(char[] inArray, int offset, int length, BaseFormatStyles options = BaseFormatStyles.None)
	{
		ThrowHelper.ThrowIfNull(inArray, nameof(inArray));
		ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(inArray, offset, length, nameof(offset), nameof(length), nameof(inArray));

		return DecodeFromHex(inArray.AsSpan(offset, length), options);
	}

	/// <summary>
	/// Decodes a hexadecimal-encoded string into a byte array.
	/// </summary>
	/// <param name="s">The string containing Base16 (hex) encoded characters.</param>
	/// <param name="options">Formatting options that allow optional prefix and whitespace to be ignored during parsing.</param>
	/// <returns>A byte array representing the decoded binary data.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="s" /> is <c>null</c>.</exception>
	/// <exception cref="FormatException">Thrown if the input is not valid Base16 format.</exception>
	public static byte[] FromBase16String(string s, BaseFormatStyles options = BaseFormatStyles.None)
	{
		ThrowHelper.ThrowIfNull(s, nameof(s));
		return DecodeFromHex(s.AsSpan(), options);
	}

	/// <summary>
	/// Encodes a portion of a byte array into hexadecimal characters and writes them into a destination character array.
	/// </summary>
	/// <param name="inArray">The input byte array to encode.</param>
	/// <param name="offsetIn">The starting index within <paramref name="inArray" />.</param>
	/// <param name="length">The number of bytes to encode.</param>
	/// <param name="outArray">The character array to receive the encoded characters.</param>
	/// <param name="offsetOut">The index in <paramref name="outArray" /> at which to begin writing.</param>
	/// <param name="options">The formatting options. Only <see cref="BaseFormattingOptions.UpperCase" /> is supported.</param>
	/// <returns>The number of characters written to <paramref name="outArray" />.</returns>
	/// <exception cref="ArgumentException">Thrown if <paramref name="options" /> includes unsupported formatting flags.</exception>
	/// <exception cref="ArgumentNullException">Thrown if any input array is <c>null</c>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offsetIn" /> or <paramref name="length" /> is invalid.</exception>
	/// <remarks>
	/// This method provides fast, allocation-free encoding into an existing buffer. It does not support prefix, line breaks, or spacing decorations.
	/// </remarks>
	public static int ToBase16CharArray(byte[] inArray, int offsetIn, int length, char[] outArray, int offsetOut, BaseFormattingOptions options = BaseFormattingOptions.None)
	{
		ThrowHelper.ThrowIfNull(inArray);
		ThrowHelper.ThrowIfNull(outArray);
		ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(inArray, offsetIn, length);
		ThrowHelper.ThrowIfArrayLengthIsInsufficient(outArray, offsetOut, length * 2);

		// Only simple upper/lower-case supported for raw char arrays.
		if ((options & ~(BaseFormattingOptions.UpperCase)) != 0)
			throw new ArgumentException("Line-breaks, spacing, or prefix are not supported when writing into a pre-allocated char array.");

		if (length == 0)
			return 0;

		bool upper = options.HasFlag(BaseFormattingOptions.UpperCase);
		EncodeToHexCore(inArray.AsSpan(offsetIn, length), outArray.AsSpan(offsetOut), upper);
		return length * 2;
	}

	/// <summary>
	/// Encodes the entire byte array into a hexadecimal string.
	/// </summary>
	/// <param name="inArray">The byte array to encode.</param>
	/// <param name="options">Formatting options to apply, such as upper case, spacing, or line breaks.</param>
	/// <returns>A formatted Base16 string representing the input bytes.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="inArray" /> is <c>null</c>.</exception>
	public static string ToBase16String(byte[] inArray, BaseFormattingOptions options = BaseFormattingOptions.None) =>
		ToBase16String(inArray, 0, inArray.Length, options);

	/// <summary>
	/// Encodes a portion of a byte array into a formatted hexadecimal string.
	/// </summary>
	/// <param name="inArray">The byte array to encode.</param>
	/// <param name="offset">The zero-based offset in <paramref name="inArray" /> to begin encoding.</param>
	/// <param name="length">The number of bytes to encode.</param>
	/// <param name="options">Formatting options to apply, such as casing, prefix, or spacing.</param>
	/// <returns>A Base16-encoded string representing the input bytes.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="inArray" /> is <c>null</c>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset" /> or <paramref name="length" /> is invalid.</exception>
	public static string ToBase16String(byte[] inArray, int offset, int length, BaseFormattingOptions options)
	{
		ThrowHelper.ThrowIfNull(inArray);
		ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(inArray, offset, length);

		return length == 0 ? string.Empty : EncodeArrayToHex(inArray, offset, length, options);
	}

	/// <summary>
	/// Encodes a read-only span of bytes into a formatted hexadecimal string.
	/// </summary>
	/// <param name="bytes">The bytes to encode.</param>
	/// <param name="options">Formatting options such as uppercase, prefix, or spacing.</param>
	/// <returns>A Base16-encoded string representing the span of bytes.</returns>
	/// <remarks>
	/// This method provides optimal performance when no formatting flags other than <see cref="BaseFormattingOptions.UpperCase" /> are specified.
	/// </remarks>
	public static string ToBase16String(ReadOnlySpan<byte> bytes, BaseFormattingOptions options = BaseFormattingOptions.None) =>
		EncodeSpanToHex(bytes, options);

	/// <summary>
	/// Attempts to decode Base16 characters into binary bytes using the provided output buffer.
	/// </summary>
	/// <param name="source">The source span of characters to decode.</param>
	/// <param name="destination">The span into which the decoded bytes will be written.</param>
	/// <param name="bytesWritten">Outputs the number of bytes successfully written.</param>
	/// <param name="options">Optional parsing flags that allow for lenient formats like whitespace and prefix.</param>
	/// <returns><c>true</c> if decoding was successful; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// This method supports both strict and lenient decoding, and will not throw for invalid input. Check the return value to ensure success.
	/// </remarks>
	public static bool TryFromBase16Chars(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten, BaseFormatStyles options = BaseFormatStyles.None)
	{
		if (options == BaseFormatStyles.None)
		{
			// strict fast-path
			ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(source, 2, true, nameof(source));

			int need = source.Length / 2;
			if (destination.Length < need)
			{
				bytesWritten = 0;
				return false;
			}

			if (!DecodeFromHexCore(source, destination))
			{
				bytesWritten = 0;
				return false;
			}

			bytesWritten = need;
			return true;
		}

		// lenient path – strip decorations first
		var tmpSpan = StripDecorations(source, options, out int digitCount);
		int needBytes = digitCount / 2;

		if (destination.Length < needBytes)
		{
			bytesWritten = 0;
			return false;
		}

		bool ok = DecodeFromHexCore(tmpSpan.Slice(0, digitCount), destination);
		bytesWritten = ok ? needBytes : 0;
		return ok;
	}

	/// <summary>
	/// Attempts to encode binary bytes into Base16 characters using the provided output span.
	/// </summary>
	/// <param name="source">The input bytes to encode.</param>
	/// <param name="destination">The span into which the encoded characters will be written.</param>
	/// <param name="charsWritten">Outputs the number of characters written.</param>
	/// <param name="options">Formatting options. Only <see cref="BaseFormattingOptions.UpperCase" /> is supported.</param>
	/// <returns><c>true</c> if encoding succeeded; otherwise, <c>false</c>.</returns>
	/// <exception cref="ArgumentException">Thrown if unsupported formatting options are used (e.g., line breaks or prefix).</exception>
	public static bool TryToBase16Chars(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten, BaseFormattingOptions options = BaseFormattingOptions.None)
	{
		if ((options & ~(BaseFormattingOptions.UpperCase)) != 0)
			throw new ArgumentException("Line-breaks, spacing, or prefix are not supported when writing into a span.");

		int needed = source.Length * 2;
		if (destination.Length < needed)
		{
			charsWritten = 0;
			return false;
		}

		bool upper = options.HasFlag(BaseFormattingOptions.UpperCase);
		EncodeToHexCore(source, destination, upper);
		charsWritten = needed;
		return true;
	}

	/// <summary>
	/// Decodes a span of Base16 (hexadecimal) characters into a byte array, optionally permitting decorations such as whitespace or prefixes.
	/// </summary>
	/// <param name="chars">The span of characters representing the Base16-encoded input.</param>
	/// <param name="options">The parsing options to apply, such as allowing the <c>0x</c> prefix or ignoring whitespace.</param>
	/// <returns>A byte array containing the decoded binary data.</returns>
	/// <exception cref="FormatException">
	/// Thrown if the input contains invalid hexadecimal characters or results in an odd number of digits after decoration stripping.
	/// </exception>
	/// <remarks>
	/// If <paramref name="options" /> is <see cref="BaseFormatStyles.None" />, the method operates in strict mode and expects an
	/// even-length, decoration-free span. Otherwise, it removes decorations before decoding.
	/// </remarks>
	private static byte[] DecodeFromHex(ReadOnlySpan<char> chars, BaseFormatStyles options)
	{
		if (options == BaseFormatStyles.None)
		{
			// strict path
			if (chars.IsEmpty)
				return Array.Empty<byte>();

			ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(chars, 2, true, nameof(chars));
			return DecodeFromHexStrict(chars);
		}

		// lenient path
		var tmp = StripDecorations(chars, options, out int digitCount);
		return DecodeFromHexStrict(tmp.Slice(0, digitCount));
	}

	/// <summary>
	/// Decodes a strict Base16-encoded character span with no decorations or formatting options.
	/// </summary>
	/// <param name="chars">The span of characters to decode (must contain only hex digits).</param>
	/// <returns>A byte array representing the decoded data.</returns>
	/// <exception cref="FormatException">Thrown if the input is not valid hexadecimal.</exception>
	/// <remarks>This overload bypasses decoration handling and assumes clean input for performance.</remarks>
	private static byte[] DecodeFromHex(ReadOnlySpan<char> chars)
	{
		if (chars.IsEmpty)
			return Array.Empty<byte>();

		byte[] result = new byte[chars.Length / 2];
		if (!DecodeFromHexCore(chars, result))
			throw new FormatException("Input string contains non-hexadecimal characters.");

		return result;
	}

	/// <summary>
	/// Core decoding logic for translating a span of hexadecimal characters into binary bytes.
	/// </summary>
	/// <param name="chars">The character span containing hex digits (must be even in length).</param>
	/// <param name="bytes">The destination span to receive the decoded bytes.</param>
	/// <returns><c>true</c> if decoding succeeds; otherwise, <c>false</c>.</returns>
	/// <remarks>This method performs fast nibble conversion and assumes validation is handled by the caller.</remarks>
	private static bool DecodeFromHexCore(ReadOnlySpan<char> chars, Span<byte> bytes)
	{
		int bi = 0;
		for (int i = 0; i < chars.Length; i += 2)
		{
			int hi = Nibble(chars[i]);
			int lo = Nibble(chars[i + 1]);
			if ((hi | lo) < 0)
				return false;

			bytes[bi++] = (byte)((hi << 4) | lo);
		}
		return true;

		static int Nibble(char c) => c switch
		{
			>= '0' and <= '9' => c - '0',
			>= 'A' and <= 'F' => c - 'A' + 10,
			>= 'a' and <= 'f' => c - 'a' + 10,
			_ => -1
		};
	}

	/// <summary>
	/// Decodes a strict hexadecimal character span into a byte array without allowing formatting or decorations.
	/// </summary>
	/// <param name="chars">The character span containing only hex digits (must be even in length).</param>
	/// <returns>The decoded byte array.</returns>
	/// <exception cref="FormatException">Thrown if any character is not a valid hexadecimal digit.</exception>
	/// <remarks>This method assumes the input has already been validated for even length and contains no formatting.</remarks>
	private static byte[] DecodeFromHexStrict(ReadOnlySpan<char> chars)
	{
		byte[] result = new byte[chars.Length / 2];
		if (!DecodeFromHexCore(chars, result))
			throw new FormatException("Input string contains non-hexadecimal characters.");
		return result;
	}

	/// <summary>
	/// Encodes a portion of a byte array into a hexadecimal string using a fast formatting path with no decorations.
	/// </summary>
	/// <param name="src">The source byte array.</param>
	/// <param name="offset">The starting index within the array.</param>
	/// <param name="length">The number of bytes to encode.</param>
	/// <param name="upper">Specifies whether to use uppercase hex characters.</param>
	/// <returns>A hexadecimal string representing the input bytes.</returns>
	/// <remarks>This method uses <see cref="string.Create" /> to minimize allocations and is used when no prefix or spacing is requested.</remarks>
	private static string EncodeArraySimpleFast(byte[] src, int offset, int length, bool upper)
	{
		return string.Create(length * 2,
							 (src, offset, upper),
							 static (Span<char> dest, (byte[] Arr, int Off, bool Upper) st) =>
							 {
								 ReadOnlySpan<char> map = (st.Upper ? HexUpperAlphabet : HexLowerAlphabet).AsSpan();
								 byte[] data = st.Arr;
								 int o = st.Off;
								 for (int i = 0; i < dest.Length / 2; i++)
								 {
									 byte b = data[o + i];
									 dest[i * 2] = map[b >> 4];
									 dest[i * 2 + 1] = map[b & 0x0F];
								 }
							 });
	}

	/// <summary>
	/// Encodes a byte array into a hexadecimal string, using either a fast or flexible formatting path.
	/// </summary>
	/// <param name="src">The byte array to encode.</param>
	/// <param name="offset">The offset into the array.</param>
	/// <param name="length">The number of bytes to encode.</param>
	/// <param name="options">Formatting options such as uppercase, spacing, or prefix.</param>
	/// <returns>A formatted Base16 string representing the input.</returns>
	/// <remarks>
	/// When no extra formatting is specified, this method uses a fast encoding path. Otherwise, it falls back to a builder-based implementation.
	/// </remarks>
	private static string EncodeArrayToHex(byte[] src, int offset, int length, BaseFormattingOptions options)
	{
		bool simpleUpper = (options & ~BaseFormattingOptions.UpperCase) == 0;
		if (simpleUpper)
			return EncodeArraySimpleFast(src, offset, length, options.HasFlag(BaseFormattingOptions.UpperCase));

		// Otherwise fall back to flexible builder
		return EncodeArrayWithFormatting(src.AsSpan(offset, length), options);
	}

	/// <summary>
	/// Encodes a span of bytes into a Base16 string with optional decorations such as spacing, line breaks, and prefix.
	/// </summary>
	/// <param name="bytes">The byte span to encode.</param>
	/// <param name="options">The formatting options to apply.</param>
	/// <returns>A formatted hexadecimal string.</returns>
	/// <remarks>
	/// This method is used when advanced formatting options like spacing or line breaks are requested. It uses a
	/// <see cref="StringBuilder" /> to construct the output.
	/// </remarks>
	private static string EncodeArrayWithFormatting(ReadOnlySpan<byte> bytes, BaseFormattingOptions options)
	{
		bool upper = options.HasFlag(BaseFormattingOptions.UpperCase);
		bool spacing = options.HasFlag(BaseFormattingOptions.InsertSpacing);
		bool lineBreaks = options.HasFlag(BaseFormattingOptions.InsertLineBreaks);
		bool prefix = options.HasFlag(BaseFormattingOptions.IncludePrefix);

		// Estimate final length --------------------------------------------------
		int charsPerByte = spacing ? 3 : 2;                 // "AA " vs "AA"
		int estimated = bytes.Length * charsPerByte;
		if (!spacing) estimated -= 0;                       // no trailing space
		if (prefix) estimated += 2;                         // "0x"
		if (lineBreaks)
		{
			int effectiveLineLen = 64;                      // break every 64 encoded chars
			int breaks = estimated / effectiveLineLen;
			if (estimated % effectiveLineLen == 0) breaks--;
			estimated += breaks * 2;                        // \r\n per break
		}

		var sb = new StringBuilder(estimated);
		if (prefix) sb.Append("0x");

		ReadOnlySpan<char> map = (upper ? HexUpperAlphabet : HexLowerAlphabet).AsSpan();
		int encodedCharsInLine = prefix ? 2 : 0;            // account for "0x" prefix

		for (int i = 0; i < bytes.Length; i++)
		{
			if (spacing && i > 0)
			{
				sb.Append(' ');
				encodedCharsInLine++;
			}

			if (lineBreaks && encodedCharsInLine >= 64)
			{
				sb.Append("\r\n");
				encodedCharsInLine = 0;
			}

			byte b = bytes[i];
			sb.Append(map[b >> 4]);
			sb.Append(map[b & 0x0F]);

			encodedCharsInLine += 2;
		}

		return sb.ToString();
	}

	/// <summary>
	/// Encodes a byte span into a hexadecimal string using a temporary character buffer.
	/// </summary>
	/// <param name="bytes">The byte span to encode.</param>
	/// <param name="upper">Indicates whether to use uppercase characters.</param>
	/// <returns>A Base16-encoded string.</returns>
	/// <remarks>This method is efficient and used when no formatting decorations are applied.</remarks>
	private static string EncodeSpanSimpleFast(ReadOnlySpan<byte> bytes, bool upper)
	{
		char[] buffer = new char[bytes.Length * 2];
		EncodeToHexCore(bytes, buffer, upper);
		return new string(buffer);
	}

	/// <summary>
	/// Encodes a byte span into a Base16 string using either fast or decorated formatting.
	/// </summary>
	/// <param name="bytes">The span of bytes to encode.</param>
	/// <param name="options">The formatting options to apply, including spacing and casing.</param>
	/// <returns>A Base16-encoded string.</returns>
	/// <remarks>
	/// If only uppercase is specified, a fast encoding path is used. Otherwise, the output is formatted with additional decorations.
	/// </remarks>
	private static string EncodeSpanToHex(ReadOnlySpan<byte> bytes, BaseFormattingOptions options)
	{
		if (bytes.IsEmpty)
			return options.HasFlag(BaseFormattingOptions.IncludePrefix) ? "0x" : string.Empty;

		// If no extra formatting flags → still use the fast path via temporary buffer
		bool simpleUpper = (options & ~BaseFormattingOptions.UpperCase) == 0;
		if (simpleUpper)
			return EncodeSpanSimpleFast(bytes, options.HasFlag(BaseFormattingOptions.UpperCase));

		// Flexible path
		return EncodeArrayWithFormatting(bytes, options);
	}

	/// <summary>
	/// Writes hexadecimal characters directly into a character span from a byte span using the specified casing.
	/// </summary>
	/// <param name="bytes">The input byte span to encode.</param>
	/// <param name="chars">The destination span to write the hexadecimal characters.</param>
	/// <param name="upperCase">Indicates whether uppercase characters should be used.</param>
	/// <remarks>This method performs fast bitwise mapping and does not allocate any intermediate data structures.</remarks>
	private static void EncodeToHexCore(ReadOnlySpan<byte> bytes, Span<char> chars, bool upperCase)
	{
		ReadOnlySpan<char> map = (upperCase ? HexUpperAlphabet : HexLowerAlphabet).AsSpan();
		for (int i = 0; i < bytes.Length; i++)
		{
			byte b = bytes[i];
			chars[i * 2] = map[b >> 4];
			chars[i * 2 + 1] = map[b & 0x0F];
		}
	}

	/// <summary>
	/// Removes optional decorations (e.g. "0x" prefix, whitespace) from a span of Base16-encoded characters.
	/// </summary>
	/// <param name="source">The source character span containing potentially decorated Base16 data.</param>
	/// <param name="options">The formatting styles to permit while parsing.</param>
	/// <param name="digitCount">Outputs the number of actual hex digits retained after filtering.</param>
	/// <returns>A span over a new character array containing the cleaned hex digits.</returns>
	/// <exception cref="FormatException">Thrown if the resulting digit count is not even.</exception>
	/// <remarks>This method allocates a temporary array only if necessary and copies the cleaned characters to a safe heap span.</remarks>
	private static ReadOnlySpan<char> StripDecorations(ReadOnlySpan<char> source, BaseFormatStyles options, out int digitCount)
	{
		// Fast scratch buffer – stack for small inputs, heap for large.
		Span<char> scratch = source.Length <= 256
			? stackalloc char[source.Length]
			: new char[source.Length];

		int j = 0;
		int i = 0;

		// Optional “0x” prefix
		if (options.HasFlag(BaseFormatStyles.AllowPrefix) &&
			source.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
		{
			i = 2;
		}

		for (; i < source.Length; i++)
		{
			char c = source[i];
			if (options.HasFlag(BaseFormatStyles.IgnoreWhitespace) &&
				(c is ' ' or '\t' or '\r' or '\n'))
			{
				continue;
			}
			scratch[j++] = c;
		}

		if ((j & 1) != 0)
			throw new FormatException("Hex digit count is not even after removing decorations.");

		digitCount = j;

		// Copy the cleaned digits into a fresh heap array so the span can escape.
		char[] result = new char[j];
		scratch.Slice(0, j).CopyTo(result);
		return result.AsSpan();
	}
}
