// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreTests.IsPstFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
}
