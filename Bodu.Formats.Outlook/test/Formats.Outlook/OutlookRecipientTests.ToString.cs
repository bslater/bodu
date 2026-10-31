// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookRecipientTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class OutlookRecipientTests
{
    /// <summary>
    /// Verifies that the diagnostic text of a recipient with a type and a display name is the type followed by the
    /// display name, the email address left out.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTypeAndDisplayNamePresent_ShouldWriteTypeAndDisplayName()
    {
        var recipient = new OutlookRecipient(new MapiPropertyCollection(new[]
        {
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.RecipientType, MapiPropertyType.Int32), (int)OutlookRecipientType.Cc),
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.DisplayName, MapiPropertyType.Unicode), "Alex"),
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.EmailAddress, MapiPropertyType.Unicode), "alex@example.com"),
        }));

        Assert.AreEqual("Cc: Alex", recipient.ToString());
    }

    /// <summary>
    /// Verifies that the diagnostic text falls back to the email address when the display name is absent, and to
    /// <c>Unknown</c> when the recipient type is absent.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTypeAndDisplayNameAbsent_ShouldWriteUnknownAndEmailAddress()
    {
        var recipient = new OutlookRecipient(new MapiPropertyCollection(new[]
        {
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.EmailAddress, MapiPropertyType.Unicode), "alex@example.com"),
        }));

        Assert.AreEqual("Unknown: alex@example.com", recipient.ToString());
    }

    /// <summary>
    /// Verifies that the diagnostic text of a recipient with no properties at all is <c>Unknown: (unnamed)</c>.
    /// </summary>
    [TestMethod]
    public void ToString_WhenNoPropertiesPresent_ShouldWriteUnknownAndUnnamed()
    {
        var recipient = new OutlookRecipient(MapiPropertyCollection.Empty);

        Assert.AreEqual("Unknown: (unnamed)", recipient.ToString());
    }

    /// <summary>
    /// Verifies that an undefined recipient-type value is written as <c>Unknown</c> while a present display name is
    /// still written.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTypeUndefinedAndDisplayNamePresent_ShouldWriteUnknownAndDisplayName()
    {
        var recipient = new OutlookRecipient(new MapiPropertyCollection(new[]
        {
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.RecipientType, MapiPropertyType.Int32), 42),
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.DisplayName, MapiPropertyType.Unicode), "Alex"),
        }));

        Assert.AreEqual("Unknown: Alex", recipient.ToString());
    }
}
