// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailAttachmentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="OutlookMailAttachment" />, the attachment view, over the synthetic mail store's
/// by-value and embedded-message attachments.
/// </summary>
[TestClass]
public partial class OutlookMailAttachmentTests
{
    /// <summary>
    /// Retrieves the synthetic full message's attachments: the by-value attachment first, the embedded-message
    /// attachment second.
    /// </summary>
    /// <param name="store">The open synthetic session.</param>
    /// <returns>The attachment views.</returns>
    internal static IReadOnlyList<OutlookMailAttachment> GetAttachments(OutlookMailStore store) =>
        OutlookMailMessageTests.GetFullMessage(store).Attachments;
}
