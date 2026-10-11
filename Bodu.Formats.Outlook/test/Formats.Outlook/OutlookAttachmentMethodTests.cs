// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookAttachmentMethodTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies that the <see cref="OutlookAttachmentMethod" /> members are pinned to the <c>PidTagAttachMethod</c> values
/// MS-OXCMSG 2.2.2.9 defines.
/// </summary>
[TestClass]
public class OutlookAttachmentMethodTests
{
    /// <summary>
    /// Verifies that each <c>PidTagAttachMethod</c> value MS-OXCMSG 2.2.2.9 defines, <c>afNone</c> (0) to
    /// <c>afByWebReference</c> (7), is the enumeration member of the expected name.
    /// </summary>
    /// <param name="name">The expected member name.</param>
    /// <param name="wireValue">The published <c>PidTagAttachMethod</c> value.</param>
    [TestMethod]
    [DataRow("None", 0)]
    [DataRow("ByValue", 1)]
    [DataRow("ByReference", 2)]
    [DataRow("ByReferenceResolve", 3)]
    [DataRow("ByReferenceOnly", 4)]
    [DataRow("EmbeddedMessage", 5)]
    [DataRow("Ole", 6)]
    [DataRow("ByWebReference", 7)]
    public void Members_WhenComparedToWireValue_ShouldMatch(string name, int wireValue)
    {
        Assert.AreEqual(name, Enum.GetName((OutlookAttachmentMethod)wireValue));
    }
}
