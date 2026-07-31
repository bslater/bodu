// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bep3CorpusTests.ValidCorpus.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

using Bodu.Test.Kat;

using static Bodu.Text.Bencode.BencodeTokenType;

namespace Bodu.Text.Bencode;

/// <summary>
/// Contains the valid half of the BEP-3 conformance corpus: canonical documents spanning the integer, byte-string,
/// list, and dictionary grammars, structural composition, and realistic wire shapes drawn from BEP 3, BEP 5, BEP 9,
/// BEP 10, BEP 12, and BEP 23. Each row pins the exact token sequence the document must produce.
/// </summary>
public sealed partial class Bep3CorpusTests
{
    /// <summary>
    /// Provides the valid corpus rows: each input document (expressed as Latin-1 text so binary payloads survive)
    /// and its expected token sequence. Programmatic rows build both the input and the expected tokens with plain
    /// loops, independent of the reader, so the expectation is not a circular oracle.
    /// </summary>
    /// <returns>The valid corpus rows.</returns>
    public static IEnumerable<object[]> Bep3CorpusValidData()
    {
        // Integer grammar — the literal BEP 3 examples plus the signed/unsigned 64-bit boundary ladder pinned by
        // libtorrent's test_bdecode.cpp and bendy's integer torture cases.
        yield return Row(new("BEP 3 positive integer example", "i3e", [Integer]));
        yield return Row(new("BEP 3 negative integer example", "i-3e", [Integer]));
        yield return Row(new("integer zero", "i0e", [Integer]));
        yield return Row(new("integer one", "i1e", [Integer]));
        yield return Row(new("integer negative one", "i-1e", [Integer]));
        yield return Row(new("integer with trailing zero", "i10e", [Integer]));
        yield return Row(new("integer with trailing zeros", "i100e", [Integer]));
        yield return Row(new("integer int32 max", "i2147483647e", [Integer]));
        yield return Row(new("integer int32 max plus one", "i2147483648e", [Integer]));
        yield return Row(new("integer int32 min", "i-2147483648e", [Integer]));
        yield return Row(new("integer int32 min minus one", "i-2147483649e", [Integer]));
        yield return Row(new("integer int64 max minus one", "i9223372036854775806e", [Integer]));
        yield return Row(new("integer int64 max", "i9223372036854775807e", [Integer]));
        yield return Row(new("integer int64 min plus one", "i-9223372036854775807e", [Integer]));
        yield return Row(new("integer int64 min", "i-9223372036854775808e", [Integer]));
        yield return Row(new("integer int64 max plus one", "i9223372036854775808e", [Integer]));
        yield return Row(new("integer mid unsigned range", "i12345678901234567890e", [Integer]));
        yield return Row(new("integer uint64 max minus one", "i18446744073709551614e", [Integer]));
        yield return Row(new("integer uint64 max", "i18446744073709551615e", [Integer]));
        yield return Row(new("integer nineteen digits", "i1000000000000000000e", [Integer]));

        // Byte-string grammar — the BEP 3 example plus structurally confusable and binary payloads; the
        // content-looks-like-a-token rows follow Transmission's benc tests and bencodepy's decoder suite.
        yield return Row(new("empty byte string", "0:", [ByteString]));
        yield return Row(new("single-byte string", "1:a", [ByteString]));
        yield return Row(new("BEP 3 byte-string example", "4:spam", [ByteString]));
        yield return Row(TwoDigitLengthRow());
        yield return Row(ThreeDigitLengthRow());
        yield return Row(new("content is end token", "1:e", [ByteString]));
        yield return Row(new("content is integer prefix", "1:i", [ByteString]));
        yield return Row(new("content is dictionary prefix", "1:d", [ByteString]));
        yield return Row(new("content is list prefix", "1:l", [ByteString]));
        yield return Row(new("content is separator", "1::", [ByteString]));
        yield return Row(new("content is digits", "3:123", [ByteString]));
        yield return Row(new("content is embedded integer document", "4:i42e", [ByteString]));
        yield return Row(new("content is embedded list document", "2:le", [ByteString]));
        yield return Row(new("content is carriage return line feed", "2:\r\n", [ByteString]));
        yield return Row(new("content is spaces", "3:   ", [ByteString]));
        yield return Row(new("content is single space", "1: ", [ByteString]));
        yield return Row(new("content is punctuation", "6:!@#$%^", [ByteString]));
        yield return Row(new("content is NUL byte", "1:\u0000", [ByteString]));
        yield return Row(new("content is 0xFF byte", "1:\u00FF", [ByteString]));
        yield return Row(FullByteRangeRow());
        yield return Row(new("content is two-byte UTF-8 sequence", "2:\u00C3\u00A9", [ByteString]));
        yield return Row(new("content is four-byte UTF-8 sequence", "4:\u00F0\u009F\u0098\u0080", [ByteString]));

        // Lists — the BEP 3 example plus width, nesting, and heterogeneous element sweeps modeled on
        // bencode-go's decoder tables.
        yield return Row(new("empty list", "le", [StartList, EndList]));
        yield return Row(new(
            "BEP 3 list example",
            "l4:spam4:eggse",
            [StartList, ByteString, ByteString, EndList]));
        yield return Row(new("list of one integer", "li3ee", [StartList, Integer, EndList]));
        yield return Row(new("list of one empty string", "l0:e", [StartList, ByteString, EndList]));
        yield return Row(new("list of one empty dictionary", "ldee", [StartList, StartDictionary, EndDictionary, EndList]));
        yield return Row(new(
            "heterogeneous list",
            "li1e3:abcledee",
            [StartList, Integer, ByteString, StartList, EndList, StartDictionary, EndDictionary, EndList]));
        yield return Row(new("list of empty strings", "l0:0:e", [StartList, ByteString, ByteString, EndList]));
        yield return Row(new("nested empty lists", "llee", [StartList, StartList, EndList, EndList]));
        yield return Row(new(
            "list of empty dictionaries",
            "ldedee",
            [StartList, StartDictionary, EndDictionary, StartDictionary, EndDictionary, EndList]));
        yield return Row(new(
            "list of populated dictionaries",
            "ld1:ai1eed1:bi2eee",
            [
                StartList,
                StartDictionary, PropertyName, Integer, EndDictionary,
                StartDictionary, PropertyName, Integer, EndDictionary,
                EndList,
            ]));
        yield return Row(new(
            "list of lists",
            "lli1eeli2eee",
            [StartList, StartList, Integer, EndList, StartList, Integer, EndList, EndList]));
        yield return Row(new(
            "triple nesting with trailing element",
            "lllei1eee",
            [StartList, StartList, StartList, EndList, Integer, EndList, EndList]));
        yield return Row(new(
            "list of three integers",
            "li1ei2ei3ee",
            [StartList, Integer, Integer, Integer, EndList]));
        yield return Row(new(
            "list of mixed scalars",
            "l4:spami-1ee",
            [StartList, ByteString, Integer, EndList]));
        yield return Row(WideListRow());
        yield return Row(DepthChainRow());

        // Dictionaries — the BEP 3 examples plus the raw-byte key-ordering ladder; the ordering edges follow
        // libtorrent's key-order checks and the signed/unsigned and UTF-16-versus-byte-order traps they encode.
        yield return Row(new("empty dictionary", "de", [StartDictionary, EndDictionary]));
        yield return Row(new(
            "BEP 3 dictionary example",
            "d3:cow3:moo4:spam4:eggse",
            [StartDictionary, PropertyName, ByteString, PropertyName, ByteString, EndDictionary]));
        yield return Row(new(
            "BEP 3 dictionary with list value example",
            "d4:spaml1:a1:bee",
            [StartDictionary, PropertyName, StartList, ByteString, ByteString, EndList, EndDictionary]));
        yield return Row(new("single integer entry", "d1:ai1ee", [StartDictionary, PropertyName, Integer, EndDictionary]));
        yield return Row(new("single empty-string value", "d1:a0:e", [StartDictionary, PropertyName, ByteString, EndDictionary]));
        yield return Row(new(
            "three sorted keys",
            "d1:a1:x1:b1:y1:c1:ze",
            [
                StartDictionary,
                PropertyName, ByteString,
                PropertyName, ByteString,
                PropertyName, ByteString,
                EndDictionary,
            ]));
        yield return Row(new(
            "prefix key before longer key",
            "d1:a1:x2:aa1:ye",
            [StartDictionary, PropertyName, ByteString, PropertyName, ByteString, EndDictionary]));
        yield return Row(new(
            "prefix and longer key with mixed values",
            "d1:a4:spam2:aai1ee",
            [StartDictionary, PropertyName, ByteString, PropertyName, Integer, EndDictionary]));
        yield return Row(new(
            "empty key sorts first",
            "d0:1:x1:a1:ye",
            [StartDictionary, PropertyName, ByteString, PropertyName, ByteString, EndDictionary]));
        yield return Row(new(
            "upper case sorts before lower case",
            "d1:B1:x1:a1:ye",
            [StartDictionary, PropertyName, ByteString, PropertyName, ByteString, EndDictionary]));
        yield return Row(new(
            "digit key sorts before letter key",
            "d1:11:x1:a1:ye",
            [StartDictionary, PropertyName, ByteString, PropertyName, ByteString, EndDictionary]));
        yield return Row(new(
            "numeric keys sort by bytes not numerically",
            "d2:101:x1:91:ye",
            [StartDictionary, PropertyName, ByteString, PropertyName, ByteString, EndDictionary]));
        yield return Row(new(
            "NUL key sorts first",
            "d1:\u00001:x1:a1:ye",
            [StartDictionary, PropertyName, ByteString, PropertyName, ByteString, EndDictionary]));
        yield return Row(new(
            "unsigned high-byte key ladder",
            "d1:\u00001:a1:\u007F1:b1:\u00801:c1:\u00FF1:de",
            [
                StartDictionary,
                PropertyName, ByteString,
                PropertyName, ByteString,
                PropertyName, ByteString,
                PropertyName, ByteString,
                EndDictionary,
            ]));
        yield return Row(new(
            "replacement character key before astral key",
            "d3:\u00EF\u00BF\u00BD1:z4:\u00F0\u009F\u0098\u00801:we",
            [StartDictionary, PropertyName, ByteString, PropertyName, ByteString, EndDictionary]));
        yield return Row(new(
            "longer keys ordered by final byte",
            "d3:abc1:x3:abd1:ye",
            [StartDictionary, PropertyName, ByteString, PropertyName, ByteString, EndDictionary]));
        yield return Row(new(
            "nested dictionary value",
            "d1:ad1:bi1eee",
            [StartDictionary, PropertyName, StartDictionary, PropertyName, Integer, EndDictionary, EndDictionary]));
        yield return Row(new(
            "empty list value",
            "d1:alee",
            [StartDictionary, PropertyName, StartList, EndList, EndDictionary]));
        yield return Row(new(
            "all four value kinds",
            "d1:ai1e1:b2:bb1:cle1:ddee",
            [
                StartDictionary,
                PropertyName, Integer,
                PropertyName, ByteString,
                PropertyName, StartList, EndList,
                PropertyName, StartDictionary, EndDictionary,
                EndDictionary,
            ]));
        yield return Row(new(
            "dictionary chain three deep",
            "d1:ad1:ad1:ai1eeee",
            [
                StartDictionary, PropertyName,
                StartDictionary, PropertyName,
                StartDictionary, PropertyName, Integer, EndDictionary,
                EndDictionary,
                EndDictionary,
            ]));
        yield return Row(new("empty key only entry", "d0:i1ee", [StartDictionary, PropertyName, Integer, EndDictionary]));
        yield return Row(new(
            "binary key and binary value",
            "d1:\u00FF1:\u0000e",
            [StartDictionary, PropertyName, ByteString, EndDictionary]));
        yield return Row(new(
            "binary values under sorted keys",
            "d1:a1:\u00001:b1:\u00FFe",
            [StartDictionary, PropertyName, ByteString, PropertyName, ByteString, EndDictionary]));

        // Structural composition — alternating container chains and sibling containers, following the nesting
        // sweeps in libtorrent's test_bdecode.cpp.
        yield return Row(new(
            "dictionary in list in dictionary",
            "d1:ald1:bi1eeee",
            [
                StartDictionary, PropertyName,
                StartList,
                StartDictionary, PropertyName, Integer, EndDictionary,
                EndList,
                EndDictionary,
            ]));
        yield return Row(new(
            "list in dictionary in list",
            "ld1:aleee",
            [StartList, StartDictionary, PropertyName, StartList, EndList, EndDictionary, EndList]));
        yield return Row(new(
            "alternating containers four deep",
            "ld1:aldeeee",
            [
                StartList,
                StartDictionary, PropertyName,
                StartList,
                StartDictionary, EndDictionary,
                EndList,
                EndDictionary,
                EndList,
            ]));
        yield return Row(new(
            "sibling containers then scalar",
            "lledei1ee",
            [StartList, StartList, EndList, StartDictionary, EndDictionary, Integer, EndList]));
        yield return Row(new(
            "dictionary with two container values",
            "d1:ale1:bdee",
            [
                StartDictionary,
                PropertyName, StartList, EndList,
                PropertyName, StartDictionary, EndDictionary,
                EndDictionary,
            ]));
        yield return Row(new(
            "empty dictionary in list in dictionary",
            "d1:aldeee",
            [StartDictionary, PropertyName, StartList, StartDictionary, EndDictionary, EndList, EndDictionary]));
        yield return Row(new(
            "list chain inside dictionary",
            "d1:allleeee",
            [
                StartDictionary, PropertyName,
                StartList, StartList, StartList, EndList, EndList, EndList,
                EndDictionary,
            ]));
        yield return Row(new(
            "mixed deep composition",
            "lli1eeld2:aai1eeee",
            [
                StartList,
                StartList, Integer, EndList,
                StartList,
                StartDictionary, PropertyName, Integer, EndDictionary,
                EndList,
                EndList,
            ]));

        // Realistic wire shapes — the metainfo and tracker structures of BEP 3/12/23 and the KRPC messages of
        // BEP 5, BEP 9, and BEP 10, with binary payloads where the real protocol carries them.
        yield return Row(SingleFileTorrentRow());
        yield return Row(new(
            "multi-file torrent info shape (BEP 3)",
            "d4:infod5:filesld6:lengthi1e4:pathl8:file.txteeeee",
            [
                StartDictionary, PropertyName,
                StartDictionary, PropertyName,
                StartList,
                StartDictionary,
                PropertyName, Integer,
                PropertyName, StartList, ByteString, EndList,
                EndDictionary,
                EndList,
                EndDictionary,
                EndDictionary,
            ]));
        yield return Row(new(
            "announce-list tiers (BEP 12)",
            "d13:announce-listll26:http://tracker.example/annel25:http://backup.example/anneee",
            [
                StartDictionary, PropertyName,
                StartList,
                StartList, ByteString, EndList,
                StartList, ByteString, EndList,
                EndList,
                EndDictionary,
            ]));
        yield return Row(new(
            "compact tracker response (BEP 23)",
            "d8:completei10e10:incompletei3e8:intervali1800e5:peers6:\u00C0\u00A8\u0001\u0001\u001A\u00E1e",
            [
                StartDictionary,
                PropertyName, Integer,
                PropertyName, Integer,
                PropertyName, Integer,
                PropertyName, ByteString,
                EndDictionary,
            ]));
        yield return Row(new(
            "tracker failure response (BEP 3)",
            "d14:failure reason20:unregistered torrente",
            [StartDictionary, PropertyName, ByteString, EndDictionary]));
        yield return Row(new(
            "KRPC ping query (BEP 5)",
            "d1:ad2:id20:abcdefghij0123456789e1:q4:ping1:t2:aa1:y1:qe",
            [
                StartDictionary,
                PropertyName, StartDictionary, PropertyName, ByteString, EndDictionary,
                PropertyName, ByteString,
                PropertyName, ByteString,
                PropertyName, ByteString,
                EndDictionary,
            ]));
        yield return Row(new(
            "KRPC ping response (BEP 5)",
            "d1:rd2:id20:mnopqrstuvwxyz123456e1:t2:aa1:y1:re",
            [
                StartDictionary,
                PropertyName, StartDictionary, PropertyName, ByteString, EndDictionary,
                PropertyName, ByteString,
                PropertyName, ByteString,
                EndDictionary,
            ]));
        yield return Row(new(
            "KRPC get_peers query (BEP 5)",
            "d1:ad2:id20:abcdefghij01234567899:info_hash20:mnopqrstuvwxyz123456e1:q9:get_peers1:t2:aa1:y1:qe",
            [
                StartDictionary,
                PropertyName, StartDictionary, PropertyName, ByteString, PropertyName, ByteString, EndDictionary,
                PropertyName, ByteString,
                PropertyName, ByteString,
                PropertyName, ByteString,
                EndDictionary,
            ]));
        yield return Row(new(
            "KRPC get_peers response with values (BEP 5)",
            "d1:rd2:id20:mnopqrstuvwxyz1234565:token8:aoeusnth6:valuesl6:\u00C0\u00A8\u0001\u0001\u001A\u00E16:\u00C0\u00A8\u0001\u0002\u001A\u00E2ee1:t2:aa1:y1:re",
            [
                StartDictionary,
                PropertyName,
                StartDictionary,
                PropertyName, ByteString,
                PropertyName, ByteString,
                PropertyName, StartList, ByteString, ByteString, EndList,
                EndDictionary,
                PropertyName, ByteString,
                PropertyName, ByteString,
                EndDictionary,
            ]));
        yield return Row(new(
            "KRPC error response (BEP 5)",
            "d1:eli201e23:A Generic Error Ocurrede1:t2:aa1:y1:ee",
            [
                StartDictionary,
                PropertyName, StartList, Integer, ByteString, EndList,
                PropertyName, ByteString,
                PropertyName, ByteString,
                EndDictionary,
            ]));
        yield return Row(new(
            "extended handshake (BEP 10)",
            "d1:md11:ut_metadatai1e6:ut_pexi2ee1:pi6881e4:reqqi250e1:v13:ExampleCliente",
            [
                StartDictionary,
                PropertyName, StartDictionary, PropertyName, Integer, PropertyName, Integer, EndDictionary,
                PropertyName, Integer,
                PropertyName, Integer,
                PropertyName, ByteString,
                EndDictionary,
            ]));
        yield return Row(new(
            "metadata request (BEP 9)",
            "d8:msg_typei0e5:piecei0ee",
            [StartDictionary, PropertyName, Integer, PropertyName, Integer, EndDictionary]));

        static object[] Row(ValidKat<string, BencodeTokenType[]> kat) => [kat];
    }

    /// <summary>
    /// Builds the two-digit-length byte-string row: a ten-byte payload whose length prefix spans two digits.
    /// </summary>
    /// <returns>The corpus row.</returns>
    private static ValidKat<string, BencodeTokenType[]> TwoDigitLengthRow() =>
        new("two-digit length prefix", "10:" + new string('a', 10), [ByteString]);

    /// <summary>
    /// Builds the three-digit-length byte-string row: a hundred-byte payload whose length prefix spans three digits.
    /// </summary>
    /// <returns>The corpus row.</returns>
    private static ValidKat<string, BencodeTokenType[]> ThreeDigitLengthRow() =>
        new("three-digit length prefix", "100:" + new string('a', 100), [ByteString]);

    /// <summary>
    /// Builds the full-byte-range row: a 256-byte string carrying every value 0x00–0xFF exactly once.
    /// </summary>
    /// <returns>The corpus row.</returns>
    private static ValidKat<string, BencodeTokenType[]> FullByteRangeRow()
    {
        var builder = new StringBuilder("256:");
        for (var value = 0; value <= 255; value++)
            builder.Append((char)value);

        return new("content is full 0x00-0xFF byte range", builder.ToString(), [ByteString]);
    }

    /// <summary>
    /// Builds the wide-list row: one hundred integer elements, with the input and the expected tokens produced by
    /// the same independent loop.
    /// </summary>
    /// <returns>The corpus row.</returns>
    private static ValidKat<string, BencodeTokenType[]> WideListRow()
    {
        var builder = new StringBuilder("l");
        var tokens = new List<BencodeTokenType> { StartList };

        for (var i = 0; i < 100; i++)
        {
            builder.Append('i').Append(i.ToString(CultureInfo.InvariantCulture)).Append('e');
            tokens.Add(Integer);
        }

        builder.Append('e');
        tokens.Add(EndList);

        return new("wide list of one hundred integers", builder.ToString(), [.. tokens]);
    }

    /// <summary>
    /// Builds the depth-chain row: lists nested exactly to the default maximum depth of 64, the deepest document the
    /// reader accepts without options.
    /// </summary>
    /// <returns>The corpus row.</returns>
    private static ValidKat<string, BencodeTokenType[]> DepthChainRow()
    {
        const int Depth = 64;
        var tokens = new BencodeTokenType[Depth * 2];

        for (var i = 0; i < Depth; i++)
        {
            tokens[i] = StartList;
            tokens[Depth + i] = EndList;
        }

        return new("list chain at default maximum depth", new string('l', Depth) + new string('e', Depth), tokens);
    }

    /// <summary>
    /// Builds the single-file torrent row: the minimal BEP 3 metainfo shape with a binary twenty-byte
    /// <c>pieces</c> payload.
    /// </summary>
    /// <returns>The corpus row.</returns>
    private static ValidKat<string, BencodeTokenType[]> SingleFileTorrentRow()
    {
        var pieces = new StringBuilder();
        for (var value = 0; value < 20; value++)
            pieces.Append((char)value);

        var input =
            "d8:announce26:http://tracker.example/ann" +
            "4:infod" +
            "6:lengthi1024e" +
            "4:name8:file.txt" +
            "12:piece lengthi16384e" +
            "6:pieces20:" + pieces +
            "ee";

        return new(
            "single-file torrent shape (BEP 3)",
            input,
            [
                StartDictionary,
                PropertyName, ByteString,
                PropertyName,
                StartDictionary,
                PropertyName, Integer,
                PropertyName, ByteString,
                PropertyName, Integer,
                PropertyName, ByteString,
                EndDictionary,
                EndDictionary,
            ]);
    }
}
