// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailMessageTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;
using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailMessageTests
{
    /// <summary>
    /// Verifies that the message conveniences and scalar properties decode from the synthetic property context, with
    /// a <c>PT_STRING8</c> value decoded under the message's declared code page rather than the store fallback.
    /// </summary>
    [TestMethod]
    [DataRow("Compatible", PstValidationLevel.Compatible)]
    [DataRow("Strict", PstValidationLevel.Strict)]
    public void Properties_WhenSyntheticStore_ShouldDecodeScalars(string testName, PstValidationLevel level)
    {
        _ = testName;

        using OutlookMailStore store = OpenSynthetic(level: level);

        OutlookMailMessage message = GetFullMessage(store);

        Assert.AreEqual(PstMessagingFixtureBuilder.SenderName, message.SenderName);
        Assert.AreEqual(PstMessagingFixtureBuilder.SenderEmailAddress, message.SenderEmailAddress);
        Assert.AreEqual("IPM.Note", message.MessageClass);
        Assert.AreEqual(true, message.Properties.GetBoolean(MapiPropertyIds.HasAttachments));
        Assert.AreEqual(
            PstMessagingFixtureBuilder.BodyText,
            message.Properties.GetString(MapiPropertyIds.Body),
            "The PT_STRING8 body must decode under the declared windows-1251 code page.");
    }

    /// <summary>
    /// Verifies that a variable-size multi-valued Unicode property decodes from the MS-PST count-plus-offset-table
    /// layout into its element strings.
    /// </summary>
    [TestMethod]
    public void Properties_WhenMultiValuedUnicode_ShouldDecodeElements()
    {
        using OutlookMailStore store = OpenSynthetic();

        string[]? values = GetFullMessage(store).Properties
            .GetStringArray(PstMessagingFixtureBuilder.MvUnicodePropertyId);

        Assert.IsNotNull(values);
        CollectionAssert.AreEqual(PstMessagingFixtureBuilder.MvUnicodeValues, values);
    }

    /// <summary>
    /// Verifies that a packed fixed-width multi-valued Int32 property decodes into its element values.
    /// </summary>
    [TestMethod]
    public void Properties_WhenMultiValuedInt32_ShouldDecodeElements()
    {
        using OutlookMailStore store = OpenSynthetic();

        var tag = new MapiPropertyTag(((uint)PstMessagingFixtureBuilder.MvInt32PropertyId << 16) | 0x1003);
        Assert.IsTrue(GetFullMessage(store).Properties.TryGetValue(tag, out MapiProperty? property));
        CollectionAssert.AreEqual(PstMessagingFixtureBuilder.MvInt32Values, (int[])property.Value!);
    }
}
