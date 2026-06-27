// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderTests.TokenType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Assertions;
using Bodu.Test.Kat;
using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeReader.TokenType" /> reports the current token kind.
/// </summary>
public partial class Utf8BencodeReaderTests
{
    /// <summary>
    /// Verifies that the token type is <see cref="BencodeTokenType.None" /> before the first read.
    /// </summary>
    [TestMethod]
    public void TokenType_WhenBeforeFirstRead_ShouldBeNone()
    {
        byte[] bytes = Bytes("i1e");
        var reader = new Utf8BencodeReader(bytes);

        Assert.AreEqual(BencodeTokenType.None, reader.TokenType);
        Assert.AreEqual(0, reader.CurrentDepth);
        Assert.AreEqual(0, reader.BytesConsumed);
    }

}
