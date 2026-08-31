// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailStoreTests
{
    /// <summary>
    /// Verifies that the store properties decode from the message-store node, are decoded once, and carry the
    /// store display name.
    /// </summary>
    [TestMethod]
    public void Properties_WhenReferenceFixture_ShouldDecodeStoreObject()
    {
        using OutlookMailStore store = OpenSample1();

        MapiPropertyCollection properties = store.Properties;

        Assert.IsTrue(properties.Count > 0, "The store object must carry decoded properties.");
        Assert.AreSame(properties, store.Properties, "The store properties must decode once and be cached.");
        Assert.AreEqual(store.Properties.GetString(MapiPropertyIds.DisplayName), store.DisplayName);
    }

    /// <summary>
    /// Verifies that the store decodes its properties under strict validation as well as the tolerant levels.
    /// </summary>
    [TestMethod]
    public void Properties_WhenStrictValidation_ShouldDecodeStoreObject()
    {
        using OutlookMailStore store = OpenSample1(PstValidationLevel.Strict);

        Assert.IsTrue(store.Properties.Count > 0);
    }
}
