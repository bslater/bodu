// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgStorageWalkerTests.Malformed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;
using Bodu.Test;

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgStorageWalkerTests
{
    /// <summary>
    /// Verifies that gaps, duplicates, unparsable suffixes, and count mismatches throw under strict validation while
    /// the tolerant level accepts the found series.
    /// </summary>
    /// <param name="testName">The scenario label.</param>
    /// <param name="firstName">The first storage name.</param>
    /// <param name="secondName">The second storage name, or empty for none.</param>
    /// <param name="declaredCount">The declared count.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow("GapInSeries", "__recip_version1.0_#00000000", "__recip_version1.0_#00000002", 2u)]
    [DataRow("UnparsableSuffix", "__recip_version1.0_#00000000", "__recip_version1.0_#NOTAHEXID", 1u)]
    [DataRow("CountMismatch", "__recip_version1.0_#00000000", "", 3u)]
    [DataRow("ShortSuffix", "__recip_version1.0_#0000", "", 0u)]
    public void EnumerateIndexed_WhenSeriesMalformedUnderStrict_ShouldThrowOutlookMsgFormatException(
        string testName, string firstName, string secondName, uint declaredCount)
    {
        _ = testName;

        var builder = new MsgFixtureBuilder()
            .AddStorage(firstName, new MsgFixtureBuilder(MsgPropertyStreamKind.RecipientOrAttachment));
        if (secondName.Length != 0)
            builder.AddStorage(secondName, new MsgFixtureBuilder(MsgPropertyStreamKind.RecipientOrAttachment));

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = EnumerateRecipientNames(builder, declaredCount, CompoundValidationLevel.Strict);
        });
    }
}
