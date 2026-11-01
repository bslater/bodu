// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookRecipientTests.RecipientType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class OutlookRecipientTests
{
    /// <summary>
    /// Verifies that every defined <c>PidTagRecipientType</c> value, from the originator to the blind-copy recipient,
    /// is reported as the corresponding <see cref="OutlookRecipientType" />.
    /// </summary>
    /// <param name="value">The stored recipient-type value.</param>
    /// <param name="expected">The recipient type the value denotes.</param>
    [TestMethod]
    [DataRow(0, OutlookRecipientType.Originator)]
    [DataRow(1, OutlookRecipientType.To)]
    [DataRow(2, OutlookRecipientType.Cc)]
    [DataRow(3, OutlookRecipientType.Bcc)]
    public void RecipientType_WhenValueIsDefined_ShouldReturnTheType(int value, OutlookRecipientType expected)
    {
        var recipient = new OutlookRecipient(new MapiPropertyCollection(new[]
        {
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.RecipientType, MapiPropertyType.Int32), value),
        }));

        Assert.AreEqual(expected, recipient.RecipientType);
    }

    /// <summary>
    /// Verifies that a recipient-type value below the originator is reported as <see langword="null" />, as a value
    /// above the blind-copy recipient is.
    /// </summary>
    [TestMethod]
    public void RecipientType_WhenValueIsBelowOriginator_ShouldReturnNull()
    {
        var recipient = new OutlookRecipient(new MapiPropertyCollection(new[]
        {
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.RecipientType, MapiPropertyType.Int32), -1),
        }));

        Assert.IsNull(recipient.RecipientType);
    }
}
