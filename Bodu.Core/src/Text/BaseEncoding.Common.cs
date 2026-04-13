// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BaseEncoding.Common.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Text;               // for StringBuilder fallback
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;

namespace Bodu.Text
{
	// ─────────────────────────────────────────────────────────────────────────────── Base16 implementation ───────────────────────────────────────────────────────────────────────────────

	public static partial class BaseEncoding
	{
		// Base16
		private const string _b16Alphabet = "0123456789ABCDEF";

		// Base32 (RFC 4648)
		private const string _b32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

		private static readonly sbyte[] _b16Lookup = BuildLookup(_b16Alphabet);

		private static readonly sbyte[] _b32Lookup = BuildLookup(_b32Alphabet);

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
}