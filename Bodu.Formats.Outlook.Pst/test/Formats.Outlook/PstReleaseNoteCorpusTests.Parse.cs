// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each cataloged file a fix established as readable is read on the selected surface into the
    /// expected rendering.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(ParseRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenFixScenarioIsRead_ShouldRenderTheExpectedText(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        string expected = CorpusEscapes.DecodeText(fix.Expected);

        string actual = RenderRow(fix);

        Assert.AreEqual(expected, actual, fix.ToString());
    }
}
