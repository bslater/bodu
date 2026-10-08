// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteCatalogTests.Load.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Kat;

namespace Bodu.Test.Corpus;

public sealed partial class ReleaseNoteCatalogTests
{
    /// <summary>
    /// Supplies one malformed row per row-level rule, with text the problem it causes must contain.
    /// </summary>
    /// <returns>The rows.</returns>
    public static IEnumerable<object[]> MalformedRows()
    {
        yield return Row("too few columns", "sample,1.0.0", "columns");
        yield return Row("unknown class", "sample,1.0.0,#1,A fix,maybe,,parse,,a,b,upstream,a reason", "class 'maybe'");
        yield return Row("unknown kind", "sample,1.0.0,#1,A fix,applies,,check,,a,b,upstream,a reason", "kind 'check'");
        yield return Row("n/a row with a kind", "sample,1.0.0,#1,A fix,n/a,,parse,,,,,packaging", "carries");
        yield return Row("empty reason", "sample,1.0.0,#1,A fix,n/a,,,,,,,", "reason");
        yield return Row("empty version", "sample,,#1,A fix,n/a,,,,,,,packaging", "version");
        yield return Row("empty summary", "sample,1.0.0,#1,,n/a,,,,,,,packaging", "summary");
        yield return Row("zero case", "sample,1.0.0,#1,A fix,n/a,0,,,,,,packaging", "case '0'");
        yield return Row("non-numeric case", "sample,1.0.0,#1,A fix,n/a,b,,,,,,packaging", "case 'b'");
        yield return Row("unknown expectation", "sample,1.0.0,#1,A fix,applies,,parse,,a,b,guess,a reason", "expectation 'guess'");
        yield return Row("oracle without a tool", "sample,1.0.0,#1,A fix,applies,,parse,,a,b,oracle:,a reason", "expectation 'oracle:'");
        yield return Row("malformed options", "sample,1.0.0,#1,A fix,applies,,parse,MaxDepth,a,b,upstream,a reason", "options");
        yield return Row("malformed input escape", @"sample,1.0.0,#1,A fix,applies,,parse,,\q,b,upstream,a reason", "input");
        yield return Row("malformed expected escape", @"sample,1.0.0,#1,A fix,applies,,parse,,a,\q,upstream,a reason", "expected");
        yield return Row("unit row without a test name", "sample,1.0.0,#1,A fix,applies,,unit,,SomeTest,,upstream,a reason", "unit row");
        yield return Row("unit row with an expected value", "sample,1.0.0,#1,A fix,applies,,unit,,FooTests.Bar_WhenX_ShouldY,b,upstream,a reason", "unit row");
        yield return Row("reject row without an exception", "sample,1.0.0,#1,A fix,applies,,reject,,a,b,upstream,a reason", "exception type");
        yield return Row("write row without text", "sample,1.0.0,#1,A fix,applies,,write,,a,,upstream,a reason", "write row");
        yield return Row("another library", "other,1.0.0,#1,A fix,n/a,,,,,,,packaging", "library 'other'");
        yield return Row("unclosed quote", "sample,1.0.0,#1,\"A fix,n/a,,,,,,,packaging", "not closed");
        yield return Row("text after a closing quote", "sample,1.0.0,#1,\"A fix\"x,n/a,,,,,,,packaging", "closing quote");
        yield return Row("quote inside an unquoted field", "sample,1.0.0,#1,A \"fix\",n/a,,,,,,,packaging", "double quote");

        static object[] Row(string name, string row, string fragment) =>
            [new ValidKat<string, string>(name, row, fragment)];
    }

    /// <summary>
    /// Verifies that a well-formed catalogue is read in full: its header lines, notes included, and every row's fields
    /// and line number, with no problems.
    /// </summary>
    [TestMethod]
    public void Load_WhenCatalogIsWellFormed_ShouldReadHeaderAndRowsWithNoProblems()
    {
        byte[] content = Encoding.ASCII.GetBytes(
            Header + "# note: a note\n" + ReleaseNoteCatalog.ColumnLine + "\n" + ValidRow + "\n"
            + "sample,0.9.0,,\"Quote a \"\"field\"\", with a comma\",n/a,,,,,,,packaging\n");

        ReleaseNoteCatalog catalog = ReleaseNoteCatalog.Load("sample-fixes.csv", content);

        Assert.AreEqual(0, catalog.Problems.Count, string.Join(Environment.NewLine, catalog.Problems));
        Assert.AreEqual("sample", catalog.Library);
        Assert.AreEqual(4, catalog.HeaderLines.Count);
        Assert.AreEqual("# note: a note", catalog.HeaderLines[3]);
        Assert.AreEqual(2, catalog.Rows.Count);

        ReleaseNoteFix first = catalog.Rows[0];
        Assert.AreEqual(6, first.LineNumber);
        Assert.AreEqual("reject", first.Kind);
        Assert.AreEqual("FormatException", first.Expected);

        ReleaseNoteFix second = catalog.Rows[1];
        Assert.AreEqual(7, second.LineNumber);
        Assert.AreEqual("Quote a \"field\", with a comma", second.Summary);
        Assert.AreEqual(string.Empty, second.Reference);
    }

    /// <summary>
    /// Verifies that a row breaking one rule is reported, once, with its file and line.
    /// </summary>
    /// <param name="kat">The malformed row and text its problem must contain.</param>
    [TestMethod]
    [DynamicData(nameof(MalformedRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Load_WhenRowBreaksOneRule_ShouldReportOneProblemAtItsLine(ValidKat<string, string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        ReleaseNoteCatalog catalog = ReleaseNoteCatalog.Load("sample-fixes.csv", Catalog(kat.Input));

        AssertSingleProblem(catalog, kat.Expected);
        Assert.IsTrue(catalog.Problems[0].StartsWith("sample-fixes.csv:5: ", StringComparison.Ordinal), catalog.Problems[0]);
    }

    /// <summary>
    /// Verifies that two rows with the same name are reported, so every row stays identifiable in test output.
    /// </summary>
    [TestMethod]
    public void Load_WhenTwoRowsShareAName_ShouldReportTheRepeat()
    {
        ReleaseNoteCatalog catalog = ReleaseNoteCatalog.Load("sample-fixes.csv", Catalog(ValidRow, ValidRow));

        AssertSingleProblem(catalog, "repeats");
    }

    /// <summary>
    /// Verifies that a file name without the <c>-fixes.csv</c> suffix is reported.
    /// </summary>
    [TestMethod]
    public void Load_WhenFileNameLacksTheSuffix_ShouldReportIt()
    {
        ReleaseNoteCatalog catalog = ReleaseNoteCatalog.Load("sample.csv", Catalog());

        Assert.IsTrue(catalog.Problems.Any(problem => problem.Contains("file name", StringComparison.Ordinal)), string.Join(Environment.NewLine, catalog.Problems));
        Assert.AreEqual(string.Empty, catalog.Library);
    }

    /// <summary>
    /// Verifies that a catalogue that does not end with a line feed is reported.
    /// </summary>
    [TestMethod]
    public void Load_WhenFileDoesNotEndWithALineFeed_ShouldReportIt()
    {
        byte[] content = Catalog(ValidRow)[..^1];

        ReleaseNoteCatalog catalog = ReleaseNoteCatalog.Load("sample-fixes.csv", content);

        AssertSingleProblem(catalog, "line feed");
    }

    /// <summary>
    /// Verifies that a byte outside printable ASCII is reported with its line.
    /// </summary>
    /// <param name="value">The offending byte.</param>
    [TestMethod]
    [DataRow((byte)0x09)]
    [DataRow((byte)0x0D)]
    [DataRow((byte)0x7F)]
    [DataRow((byte)0xE2)]
    public void Load_WhenFileHoldsAByteOutsidePrintableAscii_ShouldReportIt(byte value)
    {
        byte[] content = Catalog(ValidRow);
        content[^5] = value;

        ReleaseNoteCatalog catalog = ReleaseNoteCatalog.Load("sample-fixes.csv", content);

        Assert.IsTrue(catalog.Problems.Any(problem => problem.Contains("sample-fixes.csv:5: ", StringComparison.Ordinal) && problem.Contains("printable ASCII", StringComparison.Ordinal)), string.Join(Environment.NewLine, catalog.Problems));
    }

    /// <summary>
    /// Verifies that a missing required header line is reported.
    /// </summary>
    [TestMethod]
    public void Load_WhenARequiredHeaderLineIsMissing_ShouldReportIt()
    {
        byte[] content = Encoding.ASCII.GetBytes("# library: sample -- https://example.invalid/sample\n# licence: MIT\n" + ReleaseNoteCatalog.ColumnLine + "\n");

        ReleaseNoteCatalog catalog = ReleaseNoteCatalog.Load("sample-fixes.csv", content);

        Assert.IsTrue(catalog.Problems.Any(problem => problem.Contains("# releases:", StringComparison.Ordinal)), string.Join(Environment.NewLine, catalog.Problems));
    }

    /// <summary>
    /// Verifies that a library header that names another library is reported.
    /// </summary>
    [TestMethod]
    public void Load_WhenLibraryHeaderNamesAnotherLibrary_ShouldReportIt()
    {
        byte[] content = Encoding.ASCII.GetBytes(Header.Replace("# library: sample", "# library: other", StringComparison.Ordinal) + ReleaseNoteCatalog.ColumnLine + "\n");

        ReleaseNoteCatalog catalog = ReleaseNoteCatalog.Load("sample-fixes.csv", content);

        AssertSingleProblem(catalog, "library header");
    }

    /// <summary>
    /// Verifies that a catalogue whose column line is wrong is reported, and that no rows are read from it.
    /// </summary>
    [TestMethod]
    public void Load_WhenColumnLineIsWrong_ShouldReportItAndReadNoRows()
    {
        byte[] content = Encoding.ASCII.GetBytes(Header + "library,version\n" + ValidRow + "\n");

        ReleaseNoteCatalog catalog = ReleaseNoteCatalog.Load("sample-fixes.csv", content);

        AssertSingleProblem(catalog, "column line");
        Assert.AreEqual(0, catalog.Rows.Count);
    }

    /// <summary>
    /// Verifies that loading with a <see langword="null" /> file name throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Load_WhenFileNameIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ReleaseNoteCatalog.Load(null!, Catalog());
        });

        Assert.AreEqual("fileName", ex.ParamName);
    }
}
