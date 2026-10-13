// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReleaseNoteCorpusTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Formats.Excel;

public sealed partial class ExcelBinaryReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each cataloged input a fix established as readable renders, through the surface its row selects,
    /// to the expected sheets and cells or records.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [Timeout(RowTimeout)]
    [DynamicData(
        nameof(ParseRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenFixScenarioIsRead_ShouldRenderTheExpectedResult(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        GeneratedInput input = InputGenerator.Generate(fix.Input, options.LayoutVersion);
        string expected = CorpusEscapes.DecodeText(fix.Expected);

        string actual = Render(input, options);

        Assert.AreEqual(expected, actual, $"{fix} rendered {EncodeRendering(actual)}.");
    }
}
