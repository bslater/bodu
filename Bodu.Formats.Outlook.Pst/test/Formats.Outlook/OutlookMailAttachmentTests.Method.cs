// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailAttachmentTests.Method.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailAttachmentTests
{
    /// <summary>
    /// Verifies that a present but undefined <c>PidTagAttachMethod</c> value falls back to the by-value method under
    /// the tolerant levels when a payload is present.
    /// </summary>
    [TestMethod]
    public void Method_WhenDeclaredValueUndefined_ForCompatible_ShouldFallBackToByValue()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(static b => b.AttachMethodOverride = 99);

        Assert.AreEqual(OutlookAttachmentMethod.ByValue, GetAttachments(store)[0].Method);
    }

    /// <summary>
    /// Verifies that a present but undefined <c>PidTagAttachMethod</c> value is a format error under strict
    /// validation rather than being silently reinterpreted.
    /// </summary>
    [TestMethod]
    public void Method_WhenDeclaredValueUndefined_ForStrictValidation_ShouldThrowOutlookPstFormatException()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(
            static b => b.AttachMethodOverride = 99,
            PstValidationLevel.Strict);

        OutlookMailAttachment attachment = GetAttachments(store)[0];

        _ = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = attachment.Method;
        });
    }
}
