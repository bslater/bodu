// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgReleaseNoteCorpusTests.Reject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Formats.Outlook;

public sealed partial class MsgReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each cataloged input a fix established as malformed is rejected by the selected surface with
    /// <see cref="OutlookMsgFormatException" />, the exception the row names.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsRead_ShouldThrowOutlookMsgFormatException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = ResolveInput(fix.Input);
        Assert.AreEqual(nameof(OutlookMsgFormatException), fix.Expected, $"{fix}: a read is rejected with OutlookMsgFormatException.");

        Assert.ThrowsExactly<OutlookMsgFormatException>(
            () =>
            {
                _ = Render(input, options);
            },
            fix.ToString());
    }
}
