// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgStreamNamesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Verifies that <see cref="MsgStreamNames" /> composes the MS-OXMSG naming conventions exactly.
/// </summary>
[TestClass]
public class MsgStreamNamesTests
{
    /// <summary>
    /// Verifies that the fixed names match the specification text.
    /// </summary>
    [TestMethod]
    public void Constants_WhenComparedToSpecification_ShouldMatch()
    {
        Assert.AreEqual("__properties_version1.0", MsgStreamNames.PropertiesStreamName);
        Assert.AreEqual("__nameid_version1.0", MsgStreamNames.NameIdStorageName);
        Assert.AreEqual("__substg1.0_3701000D", MsgStreamNames.EmbeddedMessageStorageName);
    }

    /// <summary>
    /// Verifies that a value-stream name is composed from the eight-digit upper-case hexadecimal tag.
    /// </summary>
    /// <param name="testName">The scenario label.</param>
    /// <param name="tag">The raw property tag.</param>
    /// <param name="expected">The expected stream name.</param>
    [TestMethod]
    [DataRow("SubjectUnicode", 0x0037001Fu, "__substg1.0_0037001F")]
    [DataRow("AttachDataBinary", 0x37010102u, "__substg1.0_37010102")]
    [DataRow("MultiValuedKeywords", 0x3A58101Fu, "__substg1.0_3A58101F")]
    public void GetSubstgStreamName_WhenTag_ShouldComposeName(string testName, uint tag, string expected)
    {
        _ = testName;

        Assert.AreEqual(expected, MsgStreamNames.GetSubstgStreamName(tag));
    }

    /// <summary>
    /// Verifies that a multi-value element stream name appends the eight-digit element index.
    /// </summary>
    [TestMethod]
    public void GetMultiValueElementStreamName_WhenTagAndIndex_ShouldComposeName()
    {
        Assert.AreEqual("__substg1.0_3A58101F-00000000", MsgStreamNames.GetMultiValueElementStreamName(0x3A58101Fu, 0));
        Assert.AreEqual("__substg1.0_3A58101F-0000000A", MsgStreamNames.GetMultiValueElementStreamName(0x3A58101Fu, 10));
    }

    /// <summary>
    /// Verifies that recipient and attachment storage names carry the hash-prefixed eight-digit index.
    /// </summary>
    [TestMethod]
    public void GetStorageNames_WhenIndex_ShouldComposeNames()
    {
        Assert.AreEqual("__recip_version1.0_#00000000", MsgStreamNames.GetRecipientStorageName(0));
        Assert.AreEqual("__recip_version1.0_#0000000F", MsgStreamNames.GetRecipientStorageName(15));
        Assert.AreEqual("__attach_version1.0_#00000001", MsgStreamNames.GetAttachmentStorageName(1));
    }
}
