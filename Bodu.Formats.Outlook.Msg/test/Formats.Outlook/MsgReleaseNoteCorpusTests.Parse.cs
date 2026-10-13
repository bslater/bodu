// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgReleaseNoteCorpusTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Formats.Outlook;

public sealed partial class MsgReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each cataloged input a fix established as readable is read through the selected surface into the
    /// expected rendering.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(ParseRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenFixScenarioIsRead_ShouldRenderTheExpectedResult(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = ResolveInput(fix.Input);
        byte[] expected = CorpusEscapes.Decode(fix.Expected);

        byte[] actual;
        try
        {
            actual = Render(input, options);
        }
        catch (OutlookFormatException ex)
        {
            Assert.Fail($"{fix}: reading the input threw {Describe(ex)}");
            return;
        }

        Assert.AreEqual(CorpusEscapes.Encode(expected), CorpusEscapes.Encode(actual), fix.ToString());
    }
}
