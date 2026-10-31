// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiNamedPropertyTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiNamedPropertyTests
{
    /// <summary>
    /// Verifies that the diagnostic text of a numeric identity is the braced property-set GUID followed by the
    /// identifier as eight hexadecimal digits.
    /// </summary>
    [TestMethod]
    public void ToString_WhenNumericIdentity_ShouldFormatPropertySetAndHexId()
    {
        var named = new MapiNamedProperty(PublicStrings, 0x00008233u);

        Assert.AreEqual("{00020329-0000-0000-c000-000000000046}:0x00008233", named.ToString());
    }

    /// <summary>
    /// Verifies that the diagnostic text of a string identity is the braced property-set GUID followed by the name.
    /// </summary>
    [TestMethod]
    public void ToString_WhenStringIdentity_ShouldFormatPropertySetAndName()
    {
        var named = new MapiNamedProperty(PublicStrings, "Keywords");

        Assert.AreEqual("{00020329-0000-0000-c000-000000000046}:Keywords", named.ToString());
    }
}
