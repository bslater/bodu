// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bep3CorpusTests.MalformedCorpus.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Bencode;

/// <summary>
/// Contains the malformed half of the BEP-3 conformance corpus: documents that violate the integer, byte-string,
/// list, or dictionary grammar, break container balance, truncate at a structural position, carry framing or
/// trailing bytes, or defy the canonical dictionary key rules. Every row must be rejected with
/// <see cref="BencodeFormatException" />.
/// </summary>
public sealed partial class Bep3CorpusTests
{
    /// <summary>
    /// Provides the malformed corpus rows: each input document (expressed as Latin-1 text so binary payloads
    /// survive) and the exception the reader must throw. The programmatic rows build over-deep container chains
    /// with plain loops, independent of the reader.
    /// </summary>
    /// <returns>The malformed corpus rows.</returns>
    public static IEnumerable<object[]> Bep3CorpusMalformedData()
    {
        // Framing bytes — whitespace and byte-order marks are not Bencode; the BOM rows follow bendy's framing
        // torture cases and the hand-edited-file defects Transmission's tests guard against.
        yield return Row(new("whitespace-only input", " ", typeof(BencodeFormatException)));
        yield return Row(new("lone line feed", "\n", typeof(BencodeFormatException)));
        yield return Row(new("lone carriage return line feed", "\r\n", typeof(BencodeFormatException)));
        yield return Row(new("UTF-8 byte order mark alone", "\u00EF\u00BB\u00BF", typeof(BencodeFormatException)));
        yield return Row(new("UTF-8 byte order mark before document", "\u00EF\u00BB\u00BFi1e", typeof(BencodeFormatException)));
        yield return Row(new("leading space before document", " i1e", typeof(BencodeFormatException)));

        // Integer grammar — the canonical-form violations of BEP 3 ("i-0e is invalid", "leading zeros ... are
        // invalid") plus the overflow and truncation ladder from libtorrent's test_bdecode.cpp and bendy.
        yield return Row(new("leading zero", "i03e", typeof(BencodeFormatException)));
        yield return Row(new("leading zeros in list", "li03ee", typeof(BencodeFormatException)));
        yield return Row(new("leading zero as dictionary value", "d1:ai03ee", typeof(BencodeFormatException)));
        yield return Row(new("negative zero", "i-0e", typeof(BencodeFormatException)));
        yield return Row(new("negative zero in list", "li-0ee", typeof(BencodeFormatException)));
        yield return Row(new("negative leading zero", "i-03e", typeof(BencodeFormatException)));
        yield return Row(new("empty integer", "ie", typeof(BencodeFormatException)));
        yield return Row(new("lone minus", "i-e", typeof(BencodeFormatException)));
        yield return Row(new("plus sign", "i+1e", typeof(BencodeFormatException)));
        yield return Row(new("double minus", "i--1e", typeof(BencodeFormatException)));
        yield return Row(new("minus after digits", "i1-2e", typeof(BencodeFormatException)));
        yield return Row(new("leading space in integer", "i 1e", typeof(BencodeFormatException)));
        yield return Row(new("trailing space in integer", "i1 e", typeof(BencodeFormatException)));
        yield return Row(new("fractional integer", "i3.14e", typeof(BencodeFormatException)));
        yield return Row(new("exponent notation", "i1e5e", typeof(BencodeFormatException)));
        yield return Row(new("hex integer", "i0x10e", typeof(BencodeFormatException)));
        yield return Row(new("non-ASCII digits", "i\u00D9\u00A1\u00D9\u00A2e", typeof(BencodeFormatException)));
        yield return Row(new("integer prefix only", "i", typeof(BencodeFormatException)));
        yield return Row(new("truncated after minus", "i-", typeof(BencodeFormatException)));
        yield return Row(new("truncated after digits", "i12345", typeof(BencodeFormatException)));
        yield return Row(new("beyond uint64 max by one", "i18446744073709551616e", typeof(BencodeFormatException)));
        yield return Row(new("below int64 min by one", "i-9223372036854775809e", typeof(BencodeFormatException)));
        yield return Row(new("far beyond uint64 range", "i999999999999999999999999999999e", typeof(BencodeFormatException)));
        yield return Row(new("far below int64 range", "i-999999999999999999999999999999e", typeof(BencodeFormatException)));

        // Byte-string grammar — separator and length-prefix defects; the astronomical-length rows re-express
        // libtorrent's overflow probes and bencodepy's malformed-length sweep.
        yield return Row(new("lone separator", ":", typeof(BencodeFormatException)));
        yield return Row(new("separator before content", ":abc", typeof(BencodeFormatException)));
        yield return Row(new("missing separator", "4spam", typeof(BencodeFormatException)));
        yield return Row(new("wrong separator", "3;abc", typeof(BencodeFormatException)));
        yield return Row(new("negative length", "-1:x", typeof(BencodeFormatException)));
        yield return Row(new("negative length as dictionary value", "d1:a-1:xe", typeof(BencodeFormatException)));
        yield return Row(new("negative length in list", "l-1:xe", typeof(BencodeFormatException)));
        yield return Row(new("leading zero in length", "01:x", typeof(BencodeFormatException)));
        yield return Row(new("double zero length", "00:", typeof(BencodeFormatException)));
        yield return Row(new("plus sign in length", "+1:x", typeof(BencodeFormatException)));
        yield return Row(new("space inside length", "1 :x", typeof(BencodeFormatException)));
        yield return Row(new("length exceeds input", "5:ab", typeof(BencodeFormatException)));
        yield return Row(new("length exceeds input in list", "l5:abe", typeof(BencodeFormatException)));
        yield return Row(new("length exceeds input in dictionary key", "d5:abe", typeof(BencodeFormatException)));
        yield return Row(new("length with no content", "1:", typeof(BencodeFormatException)));
        yield return Row(new("length digit only", "3", typeof(BencodeFormatException)));
        yield return Row(new("length digits only", "12", typeof(BencodeFormatException)));
        yield return Row(new("length beyond int32 by one", "2147483648:x", typeof(BencodeFormatException)));
        yield return Row(new("length beyond int64", "9223372036854775808:x", typeof(BencodeFormatException)));
        yield return Row(new("length beyond uint64", "18446744073709551616:x", typeof(BencodeFormatException)));
        yield return Row(new("astronomical length", "99999999999999999999999999:x", typeof(BencodeFormatException)));

        // Container balance and the truncation matrix — a document cut at every structural position, following
        // the per-position truncation sweep in libtorrent's test_bdecode.cpp.
        yield return Row(new("lone end token", "e", typeof(BencodeFormatException)));
        yield return Row(new("double end token", "ee", typeof(BencodeFormatException)));
        yield return Row(new("unterminated empty list", "l", typeof(BencodeFormatException)));
        yield return Row(new("unterminated list with element", "li1e", typeof(BencodeFormatException)));
        yield return Row(new("outer list unterminated after inner closes", "lli1ee", typeof(BencodeFormatException)));
        yield return Row(new("unterminated empty dictionary", "d", typeof(BencodeFormatException)));
        yield return Row(new("dictionary truncated in key length", "d1", typeof(BencodeFormatException)));
        yield return Row(new("dictionary truncated after key separator", "d1:", typeof(BencodeFormatException)));
        yield return Row(new("dictionary truncated inside key", "d3:ab", typeof(BencodeFormatException)));
        yield return Row(new("dictionary truncated after key", "d1:a", typeof(BencodeFormatException)));
        yield return Row(new("dictionary truncated inside integer value", "d1:ai1", typeof(BencodeFormatException)));
        yield return Row(new("dictionary truncated inside string value", "d1:a3:x", typeof(BencodeFormatException)));
        yield return Row(new("dictionary missing final end", "d1:ai1e", typeof(BencodeFormatException)));
        yield return Row(new("dictionary value missing before end", "d1:ae", typeof(BencodeFormatException)));
        yield return Row(new("nested dictionary left open", "d1:ad1:bi1ee", typeof(BencodeFormatException)));
        yield return Row(new("dictionary left open after list value closes", "d1:ali1ee", typeof(BencodeFormatException)));
        yield return Row(new("list truncated inside integer element", "li1", typeof(BencodeFormatException)));
        yield return Row(new("list truncated inside string element", "l3:ab", typeof(BencodeFormatException)));
        yield return Row(new("deep chain missing one closer", "llllli1eeee", typeof(BencodeFormatException)));
        yield return Row(new("extra end token after nested list", "lli1eeee", typeof(BencodeFormatException)));
        yield return Row(new("list closing into nothing", "lee", typeof(BencodeFormatException)));
        yield return Row(new("torrent truncated inside pieces", "d4:infod6:pieces20:abc", typeof(BencodeFormatException)));
        yield return Row(new("torrent truncated after info key", "d4:info", typeof(BencodeFormatException)));
        yield return Row(ExcessiveDepthRow());
        yield return Row(UnclosedDeepChainRow());

        // Dictionary key rules — non-string keys, raw-byte ordering violations (including the signed-byte and
        // UTF-16-code-unit ordering traps), and duplicates; the ordering traps mirror libtorrent's key-order
        // checks and the interop defects bencode-go's decoder tests pin.
        yield return Row(new("integer key", "di1ei2ee", typeof(BencodeFormatException)));
        yield return Row(new("list key", "dlei2ee", typeof(BencodeFormatException)));
        yield return Row(new("dictionary key", "ddei2ee", typeof(BencodeFormatException)));
        yield return Row(new("integer key in nested dictionary", "ldi1ei2eee", typeof(BencodeFormatException)));
        yield return Row(new("unsorted keys", "d1:b1:x1:a1:ye", typeof(BencodeFormatException)));
        yield return Row(new("unsorted by length", "d2:aa1:x1:a1:ye", typeof(BencodeFormatException)));
        yield return Row(new("sorted case-insensitively not by raw bytes", "d1:a1:x1:B1:ye", typeof(BencodeFormatException)));
        yield return Row(new("signed-byte order trap", "d1:\u00FF1:a1:\u00001:be", typeof(BencodeFormatException)));
        yield return Row(new(
            "sorted by UTF-16 code units not raw bytes",
            "d4:\u00F0\u009F\u0098\u00801:x3:\u00EF\u00BF\u00BD1:ye",
            typeof(BencodeFormatException)));
        yield return Row(new("duplicate keys adjacent", "d1:a1:x1:a1:ye", typeof(BencodeFormatException)));
        yield return Row(new("duplicate keys separated", "d1:a1:x1:b1:y1:a1:ze", typeof(BencodeFormatException)));
        yield return Row(new("duplicate empty keys", "d0:1:x0:1:ye", typeof(BencodeFormatException)));
        yield return Row(new("empty key after non-empty key", "d1:a1:x0:1:ye", typeof(BencodeFormatException)));
        yield return Row(new("unsorted keys in inner dictionary", "d1:ad1:b1:x1:a1:yee", typeof(BencodeFormatException)));
        yield return Row(new("duplicate keys in inner dictionary", "d1:ad1:a1:x1:a1:yee", typeof(BencodeFormatException)));

        // Trailing data — bytes after a complete document, including concatenated documents and the trailing
        // newline a hand-edited torrent acquires; Transmission's benc tests pin the same rejection.
        yield return Row(new("trailing data after integer", "i1e2:xx", typeof(BencodeFormatException)));
        yield return Row(new("trailing data after string", "4:spami1e", typeof(BencodeFormatException)));
        yield return Row(new("trailing data after list", "lei1e", typeof(BencodeFormatException)));
        yield return Row(new("trailing data after dictionary", "dei1e", typeof(BencodeFormatException)));
        yield return Row(new("concatenated integers", "i1ei2e", typeof(BencodeFormatException)));
        yield return Row(new("concatenated lists", "lele", typeof(BencodeFormatException)));
        yield return Row(new("concatenated dictionaries", "dede", typeof(BencodeFormatException)));
        yield return Row(new("trailing NUL byte", "i1e\u0000", typeof(BencodeFormatException)));
        yield return Row(new("trailing line feed", "i1e\n", typeof(BencodeFormatException)));
        yield return Row(new("trailing carriage return line feed", "de\r\n", typeof(BencodeFormatException)));
        yield return Row(new("trailing space", "le ", typeof(BencodeFormatException)));
        yield return Row(new("trailing end token", "i3ee", typeof(BencodeFormatException)));
        yield return Row(new(
            "hand-edited torrent with trailing newline",
            "d8:announce3:url4:infodee\n",
            typeof(BencodeFormatException)));

        // Unknown leading bytes — every byte that cannot begin a Bencode value, at the root and nested; the
        // uppercase rows follow serde_bencode's case-sensitivity probes.
        yield return Row(new("unknown prefix byte", "x", typeof(BencodeFormatException)));
        yield return Row(new("uppercase integer prefix", "I1e", typeof(BencodeFormatException)));
        yield return Row(new("uppercase list prefix", "Le", typeof(BencodeFormatException)));
        yield return Row(new("uppercase dictionary prefix", "De", typeof(BencodeFormatException)));
        yield return Row(new("uppercase end token", "E", typeof(BencodeFormatException)));
        yield return Row(new("lone minus sign", "-", typeof(BencodeFormatException)));
        yield return Row(new("NUL prefix byte", "\u0000", typeof(BencodeFormatException)));
        yield return Row(new("0xFF prefix byte", "\u00FF", typeof(BencodeFormatException)));
        yield return Row(new("unknown prefix in list", "lxe", typeof(BencodeFormatException)));
        yield return Row(new("unknown prefix as dictionary value", "d1:axe", typeof(BencodeFormatException)));

        static object[] Row(InvalidKat<string> kat) => [kat];
    }

    /// <summary>
    /// Builds the over-deep chain row: lists nested one level past the default maximum depth of 64, which the
    /// reader must reject rather than recurse into.
    /// </summary>
    /// <returns>The corpus row.</returns>
    private static InvalidKat<string> ExcessiveDepthRow()
    {
        const int Depth = 65;

        return new(
            "list chain one past default maximum depth",
            new string('l', Depth) + new string('e', Depth),
            typeof(BencodeFormatException));
    }

    /// <summary>
    /// Builds the unclosed-deep-chain row: sixty-five list openers with no closers, which must fail on depth
    /// before the truncation is even reached.
    /// </summary>
    /// <returns>The corpus row.</returns>
    private static InvalidKat<string> UnclosedDeepChainRow() =>
        new("unclosed list chain past maximum depth", new string('l', 65), typeof(BencodeFormatException));
}
