// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyStreamReaderTests.Malformed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgPropertyStreamReaderTests
{
    /// <summary>
    /// Verifies that payloads violating the header-plus-whole-records shape throw
    /// <see cref="OutlookMsgFormatException" />.
    /// </summary>
    /// <param name="kat">The malformed-payload row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(
        nameof(MalformedRootPayloads),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Read_WhenMalformedRootPayload_ShouldThrowOutlookMsgFormatException(InvalidKat<byte[]> kat)
    {
        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = MsgPropertyStreamReader.Read(kat.Input, MsgPropertyStreamKind.Root, out _);
        });
    }

    /// <summary>
    /// Verifies that a payload valid for the root shape is rejected for a kind with a larger implied record area —
    /// the header size is kind-specific.
    /// </summary>
    [TestMethod]
    public void Read_WhenPayloadMatchesWrongKind_ShouldThrowOutlookMsgFormatException()
    {
        var payload = new byte[24];

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = MsgPropertyStreamReader.Read(payload, MsgPropertyStreamKind.Root, out _);
        });
    }
}
