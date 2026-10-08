// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CorpusEscapesTests.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

public sealed partial class CorpusEscapesTests
{
    /// <summary>
    /// Verifies that decoding a well-formed field returns its bytes.
    /// </summary>
    [TestMethod]
    public void Decode_WhenFieldIsWellFormed_ShouldReturnItsBytes()
    {
        byte[] bytes = CorpusEscapes.Decode(@"i\x3Ae");

        CollectionAssert.AreEqual(new byte[] { (byte)'i', (byte)':', (byte)'e' }, bytes);
    }

    /// <summary>
    /// Verifies that decoding a malformed field throws <see cref="FormatException" /> naming the problem.
    /// </summary>
    [TestMethod]
    public void Decode_WhenFieldIsMalformed_ShouldThrowFormatException()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = CorpusEscapes.Decode(@"\q");
        });

        Assert.IsTrue(ex.Message.Contains(@"\q", StringComparison.Ordinal), ex.Message);
    }
}
