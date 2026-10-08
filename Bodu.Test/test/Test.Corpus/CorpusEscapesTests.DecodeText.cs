// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CorpusEscapesTests.DecodeText.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

public sealed partial class CorpusEscapesTests
{
    /// <summary>
    /// Verifies that a field whose bytes are valid UTF-8 decodes to the text they spell.
    /// </summary>
    [TestMethod]
    public void DecodeText_WhenBytesAreValidUtf8_ShouldReturnTheText()
    {
        string text = CorpusEscapes.DecodeText(@"caf\u{E9}\t\xC3\xA9");

        Assert.AreEqual("caf\u00E9\t\u00E9", text);
    }

    /// <summary>
    /// Verifies that a field whose bytes are not valid UTF-8 throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void DecodeText_WhenBytesAreNotValidUtf8_ShouldThrowFormatException()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = CorpusEscapes.DecodeText(@"a\xFFb");
        });

        Assert.IsNotNull(ex.InnerException);
    }
}
