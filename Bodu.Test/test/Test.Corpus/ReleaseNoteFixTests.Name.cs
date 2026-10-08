// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteFixTests.Name.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

public sealed partial class ReleaseNoteFixTests
{
    /// <summary>
    /// Verifies that the name of a row with a reference and no case gives the library, version, reference and summary.
    /// </summary>
    [TestMethod]
    public void Name_WhenRowHasReferenceAndNoCase_ShouldNameLibraryVersionReferenceAndSummary()
    {
        ReleaseNoteFix row = Row();

        Assert.AreEqual("sample fix 1.2.3 #12: Reject a truncated document", row.Name);
    }

    /// <summary>
    /// Verifies that the name of a row with no reference leaves the reference out.
    /// </summary>
    [TestMethod]
    public void Name_WhenRowHasNoReference_ShouldOmitTheReference()
    {
        ReleaseNoteFix row = Row(reference: string.Empty);

        Assert.AreEqual("sample fix 1.2.3: Reject a truncated document", row.Name);
    }

    /// <summary>
    /// Verifies that the name of one case of a fix ends with the case number.
    /// </summary>
    [TestMethod]
    public void Name_WhenRowIsOneCaseOfSeveral_ShouldEndWithTheCaseNumber()
    {
        ReleaseNoteFix row = Row(caseNumber: "3");

        Assert.AreEqual("sample fix 1.2.3 #12: Reject a truncated document (case 3)", row.Name);
    }

    /// <summary>
    /// Verifies that a row's string form is its name followed by its file and line.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCalled_ShouldGiveTheNameFileAndLine()
    {
        ReleaseNoteFix row = Row();

        Assert.AreEqual("sample fix 1.2.3 #12: Reject a truncated document [sample-fixes.csv:7]", row.ToString());
    }
}
