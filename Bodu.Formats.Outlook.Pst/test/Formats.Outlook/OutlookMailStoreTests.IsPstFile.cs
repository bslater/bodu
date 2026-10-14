// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreTests.IsPstFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;
using Bodu.Test.IO;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailStoreTests
{
    /// <summary>
    /// Verifies that the sniff answers <see langword="true" /> for an OST file, whose header begins with the
    /// <c>!BDN</c> magic every PST variant shares, as the guides document.
    /// </summary>
    [TestMethod]
    public void IsPstFile_WhenClientMagicIsOst_ShouldReturnTrue()
    {
        byte[] input = PstReleaseNoteCorpusTests.GenerateInput("gen:Mail()|Header(client=0x4F53)");
        using var stream = new MemoryStream(input, writable: false);

        bool result = OutlookMailStore.IsPstFile(stream);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that the sniff answers from the magic alone for a prefix of the reference store, whatever is missing
    /// after it, without throwing and with the stream position restored.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="length">The number of leading bytes of the reference store the stream holds.</param>
    /// <param name="expected">Whether the prefix holds the whole <c>!BDN</c> magic.</param>
    [TestMethod]
    [DataRow("empty", 0, false)]
    [DataRow("shorter than the magic", 3, false)]
    [DataRow("the magic alone", 4, true)]
    [DataRow("truncated header", 100, true)]
    [DataRow("truncated file", 600, true)]
    public void IsPstFile_WhenStreamIsTruncated_ShouldAnswerFromTheMagicWithoutThrowing(string testName, int length, bool expected)
    {
        Assert.IsNotNull(testName);
        byte[] prefix = PstReferenceFixtures.OpenStream(Sample1).ToArray().AsSpan(0, length).ToArray();
        using var stream = new MemoryStream(prefix, writable: false);

        bool result = OutlookMailStore.IsPstFile(stream);

        Assert.AreEqual(expected, result);
        Assert.AreEqual(0, stream.Position);
    }

    /// <summary>
    /// Verifies that the sniff answers <see langword="false" /> without throwing for bytes that are not a PST file and
    /// for a stream it cannot seek.
    /// </summary>
    [TestMethod]
    public void IsPstFile_WhenStreamIsNotPstOrNotSeekable_ShouldReturnFalseWithoutThrowing()
    {
        byte[] pst = PstReleaseNoteCorpusTests.GenerateInput("gen:Mail()");

        Assert.IsFalse(OutlookMailStore.IsPstFile(new MemoryStream(new byte[1024], writable: false)));
        Assert.IsFalse(OutlookMailStore.IsPstFile(new MemoryStream([.. "not a mail store"u8], writable: false)));
        Assert.IsFalse(OutlookMailStore.IsPstFile(new NonSeekableStream(pst)));
    }
}
