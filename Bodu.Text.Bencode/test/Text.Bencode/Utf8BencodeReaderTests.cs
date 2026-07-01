// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Kat;
using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeReader" /> surfaces every Bencode token shape with the correct
/// <see cref="BencodeTokenType" /> sequence, decoded values, depth, and byte-consumption progression.
/// </summary>
[TestClass]
public partial class Utf8BencodeReaderTests
{
    /// <summary>
    /// Decodes the supplied Latin-1 text to bytes so binary content survives unchanged.
    /// </summary>
    /// <param name="text">The Latin-1 text to decode.</param>
    /// <returns>The decoded bytes.</returns>
    private static byte[] Bytes(string text) => Encoding.Latin1.GetBytes(text);

    /// <summary>
    /// Provides token-sequence known-answer rows covering scalars, empty and populated containers, and nesting.
    /// </summary>
    /// <returns>The token-sequence rows.</returns>
    public static IEnumerable<object[]> TokenSequenceCases()
    {
        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "integer",
            "i1e",
            [BencodeTokenType.Integer]));

        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "byte string",
            "3:abc",
            [BencodeTokenType.ByteString]));

        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "empty list",
            "le",
            [BencodeTokenType.StartList, BencodeTokenType.EndList]));

        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "empty dictionary",
            "de",
            [BencodeTokenType.StartDictionary, BencodeTokenType.EndDictionary]));

        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "list of scalars",
            "li1e3:abce",
            [BencodeTokenType.StartList, BencodeTokenType.Integer, BencodeTokenType.ByteString, BencodeTokenType.EndList]));

        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "single-entry dictionary",
            "d3:cow3:mooe",
            [
                BencodeTokenType.StartDictionary,
                BencodeTokenType.PropertyName,
                BencodeTokenType.ByteString,
                BencodeTokenType.EndDictionary,
            ]));

        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "nested list",
            "lli1eee",
            [
                BencodeTokenType.StartList,
                BencodeTokenType.StartList,
                BencodeTokenType.Integer,
                BencodeTokenType.EndList,
                BencodeTokenType.EndList,
            ]));

        yield return Row(new ValidKat<string, BencodeTokenType[]>(
            "dictionary with list value",
            "d4:listli1ei2eee",
            [
                BencodeTokenType.StartDictionary,
                BencodeTokenType.PropertyName,
                BencodeTokenType.StartList,
                BencodeTokenType.Integer,
                BencodeTokenType.Integer,
                BencodeTokenType.EndList,
                BencodeTokenType.EndDictionary,
            ]));

        static object[] Row(ValidKat<string, BencodeTokenType[]> kat) => [kat];
    }

}
