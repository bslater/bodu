// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReleaseNoteCorpusTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each catalogued input a fix established as readable is read by <c>Utf8DelimitedReader</c> into the
    /// expected records, or the expected header names when the row renders them.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(ParseRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenFixScenarioIsRead_ShouldRenderTheExpectedRecords(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        byte[] expected = CorpusEscapes.Decode(fix.Expected);

        string rendering = Render(ReadDocument(input, options.ToReaderOptions()), options.View);

        Assert.AreEqual(Escape(expected), Escape(rendering), fix.ToString());
    }
}
