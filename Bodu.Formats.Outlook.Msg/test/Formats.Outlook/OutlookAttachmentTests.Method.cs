// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookAttachmentTests.Method.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;
using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook;

public partial class OutlookAttachmentTests
{
    /// <summary>
    /// Builds a message with one attachment that declares <c>afByWebReference</c> (7), the method MS-OXCMSG 2.2.2.9
    /// defines for content a web service holds and the attachment references by URL.
    /// </summary>
    /// <param name="withPayload">Whether the writer also left a by-value content stream beside the reference.</param>
    /// <returns>The container stream.</returns>
    internal static MemoryStream CreateWithWebReferenceAttachment(bool withPayload) =>
        MsgFixtureBuilder.CreateMinimal()
            .AddAttachment(attachment =>
            {
                _ = attachment
                    .AddFixedEntry(0x37050003, 7)
                    .AddUnicode(MapiPropertyIds.AttachLongFilename, "plan.docx");
                if (withPayload)
                    _ = attachment.AddBinary(MapiPropertyIds.AttachData, new byte[] { 1, 2, 3 });
            })
            .Build();

    /// <summary>
    /// Verifies that a declared <c>afByWebReference</c> (7) method reports as <c>ByWebReference</c> at every validation
    /// level, rather than as an undefined value that leaves the attachment reporting <c>None</c>.
    /// </summary>
    /// <param name="level">The validation level the message is read at.</param>
    [TestMethod]
    [DataRow(CompoundValidationLevel.Compatible)]
    [DataRow(CompoundValidationLevel.Strict)]
    [DataRow(CompoundValidationLevel.Minimal)]
    public void Method_WhenDeclaredByWebReference_ShouldReportByWebReference(CompoundValidationLevel level)
    {
        using MemoryStream container = CreateWithWebReferenceAttachment(withPayload: false);

        using var message = OutlookMessage.Open(container, new OutlookMessageReaderOptions { ValidationLevel = level });

        Assert.AreEqual("ByWebReference", message.Attachments[0].Method.ToString());
    }
}
