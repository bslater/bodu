// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BaseEncoding.Common.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Text;               // for StringBuilder fallback
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;

namespace Bodu.Text;

// ─────────────────────────────────────────────────────────────────────────────── Base16 implementation ───────────────────────────────────────────────────────────────────────────────

public static partial class BaseEncoding
{
	// Base16
	private const string _b16Alphabet = "0123456789ABCDEF";

	// Base32 (RFC 4648)
	private const string _b32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

	private static readonly sbyte[] s_b16Lookup = BuildLookup(_b16Alphabet);

	private static readonly sbyte[] s_b32Lookup = BuildLookup(_b32Alphabet);

	/// <summary>
	/// Builds a 128-entry lookup table mapping alphabet characters to their numeric symbol index.
	/// </summary>
	/// <param name="alphabet">The encoding alphabet; each character's position is its symbol value.</param>
	/// <param name="acceptLower">When <see langword="true" />, case-folds letter characters so that
	/// the lower-case variant maps to the same symbol as the upper-case variant.</param>
	/// <returns>A 128-entry <see cref="sbyte" /> array where valid characters map to their symbol
	/// index (0..<paramref name="alphabet" />.Length-1) and all other entries are <c>-1</c>.</returns>
	private static sbyte[] BuildLookup(string alphabet, bool acceptLower = true)
	{
		var table = new sbyte[128];
		Array.Fill(table, (sbyte)-1);
		for (int i = 0; i < alphabet.Length; i++)
		{
			char c = alphabet[i];
			table[c] = (sbyte)i;
			if (acceptLower && char.IsLetter(c))
				table[char.ToLowerInvariant(c)] = (sbyte)i;
		}
		return table;
	}

	/// <summary>
	/// Encodes a byte buffer into a radix-aware character string using the supplied alphabet.
	/// </summary>
	/// <param name="data">The bytes to encode.</param>
	/// <param name="alphabet">The encoding alphabet; its length must be a power of two (equal to
	/// 2<sup><paramref name="bitsPerSymbol" /></sup>).</param>
	/// <param name="bitsPerSymbol">The number of bits each output symbol represents (for example
	/// 4 for Base16, 5 for Base32).</param>
	/// <param name="fmt">Formatting flags influencing casing of the output.</param>
	/// <returns>The encoded string. A byte-aligned fast path is used when 8 is divisible by
	/// <paramref name="bitsPerSymbol" />; otherwise a generic bit-stream path emits a final
	/// padded symbol per RFC 4648.</returns>
	private static string EncodeCore(ReadOnlySpan<byte> data, ReadOnlySpan<char> alphabet, int bitsPerSymbol, BaseFormattingOptions fmt)
	{
		bool upper = fmt.HasFlag(BaseFormattingOptions.UpperCase);
		if (upper && alphabet.Length <= 36)           // simple case-fold for A–Z
			alphabet = alphabet.ToString().ToUpperInvariant().AsSpan();

		// Fast byte-aligned path (radix 16, 8, 256 …)
		if (8 % bitsPerSymbol == 0)
		{
			int symbolsPerByte = 8 / bitsPerSymbol;
			return string.Create(data.Length * symbolsPerByte, (data, alphabet, bitsPerSymbol),
				static (span, state) =>
				{
					int mask = (1 << state.bitsPerSymbol) - 1;
					for (int i = 0, s = 0; i < state.data.Length; i++)
					{
						byte b = state.data[i];
						for (int shift = (8 - state.bitsPerSymbol); shift >= 0; shift -= state.bitsPerSymbol, s++)
							span[s] = state.alphabet[(b >> shift) & mask];
					}
				});
		}

		// Generic bit-stream path (Base32, Base58, …)
		var sb = new StringBuilder((int)Math.Ceiling(data.Length * 8 / (double)bitsPerSymbol));
		int acc = 0, accBits = 0, maskBits = (1 << bitsPerSymbol) - 1;

		foreach (byte b in data)
		{
			acc = (acc << 8) | b;
			accBits += 8;

			while (accBits >= bitsPerSymbol)
			{
				accBits -= bitsPerSymbol;
				sb.Append(alphabet[(acc >> accBits) & maskBits]);
			}
		}

		if (accBits > 0)  // pad final symbol (RFC 4648 style)
			sb.Append(alphabet[(acc << (bitsPerSymbol - accBits)) & maskBits]);

		return sb.ToString();
	}

	/// <summary>
	/// Attempts to decode a radix-encoded character sequence into a byte span, tolerating
	/// optional decorations (whitespace, <c>0x</c> prefix) according to <paramref name="style" />.
	/// </summary>
	/// <param name="text">The encoded input characters.</param>
	/// <param name="lookup">The 128-entry symbol lookup table produced by
	/// <see cref="BuildLookup(string, bool)" />.</param>
	/// <param name="bitsPerSymbol">The number of bits each input symbol contributes.</param>
	/// <param name="dest">The destination span that receives the decoded bytes.</param>
	/// <param name="bytesWritten">When this method returns, contains the number of bytes written
	/// to <paramref name="dest" />, or zero if decoding failed.</param>
	/// <param name="style">Formatting allowances applied during scanning of
	/// <paramref name="text" />.</param>
	/// <returns><see langword="true" /> on successful decode; <see langword="false" /> when an
	/// invalid symbol is encountered or <paramref name="dest" /> is too small.</returns>
	private static bool TryDecodeCore(ReadOnlySpan<char> text, ReadOnlySpan<sbyte> lookup, int bitsPerSymbol, Span<byte> dest, out int bytesWritten, BaseFormatStyles style)
	{
		bytesWritten = 0;
		int acc = 0, accBits = 0;

		foreach (char c in text)
		{
			// Decorations
			if (style.HasFlag(BaseFormatStyles.IgnoreWhitespace) &&
				(c is ' ' or '\t' or '\r' or '\n'))
				continue;

			if (style.HasFlag(BaseFormatStyles.AllowPrefix) && bytesWritten == 0 &&
				(c == '0' || c == 'x' || c == 'X'))          // extremely simple prefix skip
				continue;

			if (c >= lookup.Length) return false;
			int val = lookup[c];
			if (val < 0) return false;

			acc = (acc << bitsPerSymbol) | val;
			accBits += bitsPerSymbol;

			if (accBits >= 8)
			{
				accBits -= 8;
				if (bytesWritten == dest.Length) return false;
				dest[bytesWritten++] = (byte)(acc >> accBits);
				acc &= (1 << accBits) - 1;
			}
		}
		return accBits == 0;
	}
}
