// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeKnownAnswerVectors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Provides Known Answer Test vectors for the Bencode format sourced from BEP 3 (the canonical bencoding
/// specification), the Wikipedia Bencode article, and curated edge cases. Each provider yields rows in the
/// shape expected by MSTest's <c>[DynamicData]</c> attribute.
/// </summary>
public static class BencodeKnownAnswerVectors
{

    /// <summary>
    /// BEP 3 citation source for vectors derived from the canonical specification.
    /// </summary>
    private const string Bep3 = "BEP 3";

    /// <summary>
    /// Curated-edge-case citation source.
    /// </summary>
    private const string EdgeCase = "Edge case";

    /// <summary>
    /// Wikipedia citation source for examples mirrored from the public article.
    /// </summary>
    private const string Wikipedia = "Wikipedia: Bencode";

    /// <summary>
    /// Returns the concatenation of all positive vector providers, used by round-trip property tests.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> AllPositiveVectors()
    {
        foreach (object[] row in Bep3PositiveVectors())
            yield return row;

        foreach (object[] row in WikipediaVectors())
            yield return row;

        foreach (object[] row in EdgeCasePositiveVectors())
            yield return row;
    }

    /// <summary>
    /// Returns negative vectors covering each rule that <see cref="Bencode" /> enforces during decoding.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> Bep3NegativeVectors()
    {
        yield return new object[] { BencodeNegativeDecodeVector.Ascii("Integer with leading zero (i03e)", "i03e", nameof(FormatsResourceStrings.Format_Invalid_BencodeLeadingZerosInteger), Bep3) };
        yield return new object[] { BencodeNegativeDecodeVector.Ascii("Negative zero integer (i-0e)", "i-0e", nameof(FormatsResourceStrings.Format_Invalid_BencodeNegativeZeroInteger), Bep3) };
        yield return new object[] { BencodeNegativeDecodeVector.Ascii("Unterminated integer (i42)", "i42", nameof(FormatsResourceStrings.Format_Invalid_BencodeUnterminatedInteger), Bep3) };
        yield return new object[] { BencodeNegativeDecodeVector.Ascii("Integer with minus only (i-e)", "i-e", nameof(FormatsResourceStrings.Format_Invalid_BencodeInvalidInteger), Bep3) };
        yield return new object[] { BencodeNegativeDecodeVector.Ascii("Empty integer body (ie)", "ie", nameof(FormatsResourceStrings.Format_Invalid_BencodeInvalidInteger), Bep3) };
        yield return new object[] { BencodeNegativeDecodeVector.Ascii("Integer overflows Int64 (i99…e)", "i99999999999999999999e", nameof(FormatsResourceStrings.Format_Invalid_BencodeIntegerOutOfRange), Bep3) };

        yield return new object[] { BencodeNegativeDecodeVector.Ascii("String length exceeds input (4:spa)", "4:spa", nameof(FormatsResourceStrings.Format_Invalid_BencodeStringLengthExceedsInput), Bep3) };
        yield return new object[] { BencodeNegativeDecodeVector.Ascii("String missing colon (4spam)", "4spam", nameof(FormatsResourceStrings.Format_Invalid_BencodeStringMissingSeparator), Bep3) };
        yield return new object[] { BencodeNegativeDecodeVector.Ascii("String length with leading zero (04:spam)", "04:spam", nameof(FormatsResourceStrings.Format_Invalid_BencodeStringLengthLeadingZeros), Bep3) };
        yield return new object[] { BencodeNegativeDecodeVector.Ascii("String length overflows Int32 (9999999999:)", "9999999999:", nameof(FormatsResourceStrings.Format_Invalid_BencodeStringLengthTooLarge), Bep3) };

        yield return new object[] { BencodeNegativeDecodeVector.Ascii("Unknown token (x)", "x", nameof(FormatsResourceStrings.Format_Invalid_BencodeUnexpectedToken), Bep3) };
        yield return new object[] { BencodeNegativeDecodeVector.Ascii("Empty input", string.Empty, nameof(FormatsResourceStrings.Format_Invalid_BencodeUnexpectedEndOfData), Bep3) };
        yield return new object[] { BencodeNegativeDecodeVector.Ascii("Trailing data after value (i3e0:trailing)", "i3e0:trailing", nameof(FormatsResourceStrings.Format_Invalid_BencodeTrailingData), Bep3) };

        yield return new object[] { BencodeNegativeDecodeVector.Ascii("Unterminated list (l1:a)", "l1:a", nameof(FormatsResourceStrings.Format_Invalid_BencodeUnterminatedList), Bep3) };
        yield return new object[] { BencodeNegativeDecodeVector.Ascii("Unterminated dictionary (d1:a1:b)", "d1:a1:b", nameof(FormatsResourceStrings.Format_Invalid_BencodeUnterminatedDictionary), Bep3) };
        yield return new object[] { BencodeNegativeDecodeVector.Ascii("Dictionary with non-string key (di1e1:ve)", "di1e1:ve", nameof(FormatsResourceStrings.Format_Invalid_BencodeNonStringDictionaryKey), Bep3) };
        yield return new object[] { BencodeNegativeDecodeVector.Ascii("Dictionary with unsorted keys (b before a)", "d1:b0:1:a0:e", nameof(FormatsResourceStrings.Format_Invalid_BencodeUnorderedDictionaryKeys), Bep3) };
        yield return new object[] { BencodeNegativeDecodeVector.Ascii("Dictionary with duplicate keys", "d1:a0:1:a0:e", nameof(FormatsResourceStrings.Format_Invalid_BencodeUnorderedDictionaryKeys), Bep3) };
    }

    /// <summary>
    /// Returns positive vectors derived from BEP 3 (BitTorrent Enhancement Proposal 3), the canonical bencoding
    /// specification.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> Bep3PositiveVectors()
    {
        yield return new object[] { BencodeKnownAnswerVector.Integer(3, "i3e", Bep3) };
        yield return new object[] { BencodeKnownAnswerVector.Integer(-3, "i-3e", Bep3) };
        yield return new object[] { BencodeKnownAnswerVector.Utf8String("spam", "4:spam", Bep3) };

        BencodedList listSpamAndInt = new(new BencodedValue[]
        {
            BencodedString.FromUtf8("spam"),
            new BencodedInteger(42),
        });
        yield return new object[] { BencodeKnownAnswerVector.List("List [\"spam\", 42]", "l4:spami42ee", listSpamAndInt, Bep3) };

        BencodedDictionary cowMooSpamEggs = new(new[]
        {
            new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("cow"), BencodedString.FromUtf8("moo")),
            new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("spam"), BencodedString.FromUtf8("eggs")),
        });
        yield return new object[] { BencodeKnownAnswerVector.Dictionary("Dictionary {cow:moo, spam:eggs}", "d3:cow3:moo4:spam4:eggse", cowMooSpamEggs, Bep3) };

        BencodedDictionary spamAB = new(new[]
        {
            new KeyValuePair<BencodedString, BencodedValue>(
                BencodedString.FromUtf8("spam"),
                new BencodedList(new BencodedValue[]
                {
                    BencodedString.FromUtf8("a"),
                    BencodedString.FromUtf8("b"),
                })),
        });
        yield return new object[] { BencodeKnownAnswerVector.Dictionary("Dictionary {spam:[a,b]}", "d4:spaml1:a1:bee", spamAB, Bep3) };
    }

    /// <summary>
    /// Returns curated edge-case positive vectors: extreme integers, nested structures, raw byte payloads.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> EdgeCasePositiveVectors()
    {
        yield return new object[] { BencodeKnownAnswerVector.Integer(long.MaxValue, "i9223372036854775807e", EdgeCase) };
        yield return new object[] { BencodeKnownAnswerVector.Integer(long.MinValue, "i-9223372036854775808e", EdgeCase) };
        yield return new object[] { BencodeKnownAnswerVector.Integer(-1, "i-1e", EdgeCase) };

        BencodedString singleZero = new(new byte[] { 0x00 });
        yield return new object[] { BencodeKnownAnswerVector.FromHex("Single null byte string", "313a00", singleZero, EdgeCase) };

        byte[] highByteValues = { 0xFF, 0x00, 0x7E, 0x01 };
        BencodedString rawBytes = new(highByteValues);
        yield return new object[] { BencodeKnownAnswerVector.FromHex("Four high/low bytes", "343aff007e01", rawBytes, EdgeCase) };

        BencodedList nested = new(new BencodedValue[]
        {
            new BencodedList(new BencodedValue[]
            {
                new BencodedList(new BencodedValue[]
                {
                    new BencodedInteger(1),
                }),
            }),
        });
        yield return new object[] { BencodeKnownAnswerVector.List("Deeply nested empty/integer lists", "llli1eeee", nested, EdgeCase) };

        BencodedList dictInList = new(new BencodedValue[]
        {
            new BencodedDictionary(new[]
            {
                new KeyValuePair<BencodedString, BencodedValue>(BencodedString.FromUtf8("k"), new BencodedInteger(7)),
            }),
        });
        yield return new object[] { BencodeKnownAnswerVector.List("Dictionary inside list", "ld1:ki7eee", dictInList, EdgeCase) };
    }

    /// <summary>
    /// Returns positive vectors derived from the Wikipedia Bencode article: empty containers, zero, empty string.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> WikipediaVectors()
    {
        yield return new object[] { BencodeKnownAnswerVector.Integer(0, "i0e", Wikipedia) };
        yield return new object[] { BencodeKnownAnswerVector.Utf8String(string.Empty, "0:", Wikipedia) };
        yield return new object[] { new BencodeKnownAnswerVector("Empty list", System.Text.Encoding.ASCII.GetBytes("le"), new BencodedList(Array.Empty<BencodedValue>()), Wikipedia) };
        yield return new object[] { new BencodeKnownAnswerVector("Empty dictionary", System.Text.Encoding.ASCII.GetBytes("de"), new BencodedDictionary(Array.Empty<KeyValuePair<BencodedString, BencodedValue>>()), Wikipedia) };
    }

}
