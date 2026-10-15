// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReleaseNoteCorpusTests.Reject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Biff;
using Bodu.IO.Compound;
using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Formats.Excel;

public sealed partial class ExcelBinaryReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each cataloged input a fix established as malformed or unsupported is rejected, through the
    /// surface its row selects, with exactly the exception the row names.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod(UnfoldingStrategy = TestDataSourceUnfoldingStrategy.Unfold)]
    [TestCategory("Regression")]
    [Timeout(RowTimeout)]
    [DynamicData(
        nameof(RejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsRead_ShouldThrowTheNamedException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        GeneratedInput input = InputGenerator.Generate(fix.Input, options.LayoutVersion);
        string[] named = options.ReadsRecords ? s_recordsExceptions : s_workbookExceptions;
        Assert.Contains(fix.Expected, named, $"{fix}: this surface is not rejected with {fix.Expected}.");

        switch (fix.Expected)
        {
            case nameof(ExcelBinaryFormatException):
                AssertRejects<ExcelBinaryFormatException>(fix, input, options);
                break;

            case nameof(ExcelBinaryUnsupportedException):
                AssertRejects<ExcelBinaryUnsupportedException>(fix, input, options);
                break;

            case nameof(ExcelBinaryEncryptedWorkbookException):
                AssertRejects<ExcelBinaryEncryptedWorkbookException>(fix, input, options);
                break;

            case nameof(ExcelBinaryWorkbookStreamNotFoundException):
                AssertRejects<ExcelBinaryWorkbookStreamNotFoundException>(fix, input, options);
                break;

            case nameof(CompoundFileFormatException):
                AssertRejects<CompoundFileFormatException>(fix, input, options);
                break;

            case nameof(BiffFormatException):
                AssertRejects<BiffFormatException>(fix, input, options);
                break;

            case nameof(BiffUnsupportedVersionException):
                AssertRejects<BiffUnsupportedVersionException>(fix, input, options);
                break;
        }
    }
}
