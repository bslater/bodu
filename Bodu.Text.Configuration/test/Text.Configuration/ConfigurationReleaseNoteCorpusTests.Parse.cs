// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationReleaseNoteCorpusTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Configuration;

public sealed partial class ConfigurationReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each catalogued input a fix established as readable parses under the row's profile and resolves,
    /// for the row's target path, to the expected view.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(ParseRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenFixScenarioIsResolved_ShouldRenderTheExpectedView(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        var options = RowOptions.Parse(fix.Options);
        string text = CorpusEscapes.DecodeText(fix.Input);
        string expected = CorpusEscapes.DecodeText(fix.Expected);

        string actual = RenderOrDescribe(text, options);

        Assert.AreEqual(expected, actual, fix.ToString());
    }
}
