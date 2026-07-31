// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookRecipientTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="OutlookRecipient" />, the recipient view.
/// </summary>
[TestClass]
public class OutlookRecipientTests
{
    /// <summary>
    /// Verifies that the conveniences surface their underlying properties.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPropertiesPresent_ShouldSurfaceConveniences()
    {
        var recipient = new OutlookRecipient(new MapiPropertyCollection(new[]
        {
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.RecipientType, MapiPropertyType.Int32), (int)OutlookRecipientType.Bcc),
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.DisplayName, MapiPropertyType.Unicode), "Alex"),
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.EmailAddress, MapiPropertyType.Unicode), "alex@example.com"),
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.AddressType, MapiPropertyType.Unicode), "SMTP"),
        }));

        Assert.AreEqual(OutlookRecipientType.Bcc, recipient.RecipientType);
        Assert.AreEqual("Alex", recipient.DisplayName);
        Assert.AreEqual("alex@example.com", recipient.EmailAddress);
        Assert.AreEqual("SMTP", recipient.AddressType);
    }

    /// <summary>
    /// Verifies that absent properties surface as <see langword="null" />, and that an undefined recipient-type value
    /// is reported as <see langword="null" /> rather than an out-of-range enum.
    /// </summary>
    [TestMethod]
    public void RecipientType_WhenAbsentOrUndefined_ShouldReturnNull()
    {
        var empty = new OutlookRecipient(MapiPropertyCollection.Empty);
        Assert.IsNull(empty.RecipientType);
        Assert.IsNull(empty.DisplayName);

        var undefined = new OutlookRecipient(new MapiPropertyCollection(new[]
        {
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.RecipientType, MapiPropertyType.Int32), 42),
        }));
        Assert.IsNull(undefined.RecipientType);
    }
}
