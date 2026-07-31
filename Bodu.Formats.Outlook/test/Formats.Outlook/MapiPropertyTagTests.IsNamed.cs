// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyTagTests.IsNamed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyTagTests
{
    /// <summary>
    /// Verifies that identifiers at or above <c>0x8000</c> are reported as named properties.
    /// </summary>
    /// <param name="testName">The scenario label.</param>
    /// <param name="id">The property identifier.</param>
    /// <param name="expected">The expected named state.</param>
    [TestMethod]
    [DataRow("StandardId", (ushort)0x0037, false)]
    [DataRow("HighestStandardId", (ushort)0x7FFF, false)]
    [DataRow("NamedFloor", (ushort)0x8000, true)]
    [DataRow("HighNamedId", (ushort)0xFFFE, true)]
    public void IsNamed_WhenIdInNamedRange_ShouldReflectRange(string testName, ushort id, bool expected)
    {
        _ = testName;

        Assert.AreEqual(expected, new MapiPropertyTag(id, MapiPropertyType.Unicode).IsNamed);
    }
}
