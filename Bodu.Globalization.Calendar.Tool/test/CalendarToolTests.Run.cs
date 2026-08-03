// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarToolTests.Run.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Tool;

public sealed partial class CalendarToolTests
{
    /// <summary>
    /// Verifies that no arguments, help flags, and unknown commands print usage on the error writer and exit with the
    /// usage code.
    /// </summary>
    /// <param name="argument">The first argument, or empty for none.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow("--help")]
    [DataRow("help")]
    [DataRow("frobnicate")]
    public void Run_WhenUsageIsWrong_ShouldPrintUsageAndExitTwo(string argument)
    {
        (int exitCode, _, string error) = RunTool(argument.Length == 0 ? [] : [argument]);

        Assert.AreEqual(CalendarTool.ExitUsage, exitCode);
        Assert.IsTrue(error.Contains("bodu-calendar", StringComparison.Ordinal), "Usage help must be printed.");
    }

    /// <summary>
    /// Verifies that linting a valid document reports OK with exit code zero.
    /// </summary>
    [TestMethod]
    public void Run_WhenLintingValidDocument_ShouldExitZero()
    {
        string path = WriteScratchFile("lint-valid.xml", ValidXml);

        (int exitCode, string output, _) = RunTool("lint", path);

        Assert.AreEqual(CalendarTool.ExitSuccess, exitCode);
        Assert.IsTrue(output.Contains("tool.valid", StringComparison.Ordinal));
        Assert.IsTrue(output.Contains("OK", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that linting an invalid document prints the stable diagnostic codes and exits with the failure code.
    /// </summary>
    [TestMethod]
    public void Run_WhenLintingInvalidDocument_ShouldPrintDiagnosticsAndExitOne()
    {
        string path = WriteScratchFile("lint-invalid.xml", InvalidXml);

        (int exitCode, string output, _) = RunTool("lint", path);

        Assert.AreEqual(CalendarTool.ExitFailure, exitCode);
        Assert.IsTrue(output.Contains("BODU-CAL-ALGORITHM", StringComparison.Ordinal), "The stable diagnostic code must be printed.");
        Assert.IsTrue(output.Contains("FAILED", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that linting a missing file exits with the failure code and names the file.
    /// </summary>
    [TestMethod]
    public void Run_WhenLintingMissingFile_ShouldExitOne()
    {
        (int exitCode, _, string error) = RunTool("lint", Path.Combine(Scratch, "does-not-exist.xml"));

        Assert.AreEqual(CalendarTool.ExitFailure, exitCode);
        Assert.IsTrue(error.Contains("does-not-exist.xml", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that compiling a valid document produces a pack that loads with resolution parity, and that the info
    /// verb summarizes it.
    /// </summary>
    [TestMethod]
    public void Run_WhenCompilingValidDocument_ShouldProduceLoadablePackAndInfo()
    {
        string input = WriteScratchFile("compile-valid.xml", ValidXml);
        string packPath = Path.Combine(Scratch, "compile-valid.bcal");

        (int compileExit, string compileOutput, _) = RunTool("compile", input, "-o", packPath);

        Assert.AreEqual(CalendarTool.ExitSuccess, compileExit);
        Assert.IsTrue(File.Exists(packPath), "The pack file must be written.");
        Assert.IsTrue(compileOutput.Contains("tool.valid", StringComparison.Ordinal));

        using (FileStream stream = File.OpenRead(packPath))
        {
            NotableDateResource loaded = NotableDateResourceLoader.LoadBinary(stream);
            Assert.AreEqual("tool.valid", loaded.ResourceId);
            NotableDateService service = new(loaded);
            Assert.HasCount(1, service.Resolve(new DateOnly(2026, 1, 1), "XX"));
        }

        (int infoExit, string infoOutput, _) = RunTool("info", packPath);

        Assert.AreEqual(CalendarTool.ExitSuccess, infoExit);
        Assert.IsTrue(infoOutput.Contains("tool.valid", StringComparison.Ordinal));
        Assert.IsTrue(infoOutput.Contains("format v1", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that compiling defaults the output path to the input with a <c>.bcal</c> extension.
    /// </summary>
    [TestMethod]
    public void Run_WhenCompilingWithoutOutputOption_ShouldDefaultToBcalExtension()
    {
        string input = WriteScratchFile("compile-default.xml", ValidXml);

        (int exitCode, _, _) = RunTool("compile", input);

        Assert.AreEqual(CalendarTool.ExitSuccess, exitCode);
        Assert.IsTrue(File.Exists(Path.ChangeExtension(input, ".bcal")));
    }

    /// <summary>
    /// Verifies that compiling an invalid document writes no pack, prints diagnostics on the error writer, and exits
    /// with the failure code.
    /// </summary>
    [TestMethod]
    public void Run_WhenCompilingInvalidDocument_ShouldExitOneWithoutPack()
    {
        string input = WriteScratchFile("compile-invalid.xml", InvalidXml);
        string packPath = Path.Combine(Scratch, "compile-invalid.bcal");

        (int exitCode, _, string error) = RunTool("compile", input, "-o", packPath);

        Assert.AreEqual(CalendarTool.ExitFailure, exitCode);
        Assert.IsFalse(File.Exists(packPath), "No pack may be written for an invalid document.");
        Assert.IsTrue(error.Contains("BODU-CAL-ALGORITHM", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that imports resolve from the resolver directory ahead of the bundled catalogues.
    /// </summary>
    [TestMethod]
    public void Run_WhenLintingWithResolverDirectory_ShouldResolveImports()
    {
        const string importedXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="tool.shared">
              <NotableDates>
                <NotableDate id="shared-day" displayName="Shared Day" category="Observance">
                  <Rules><Rule id="x"><Strategy><Fixed month="June" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        const string importingXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="tool.importing">
              <Imports><Import resource="tool-shared" /></Imports>
              <NotableDates>
                <NotableDate id="local-day" displayName="Local Day" category="Observance">
                  <Rules><Rule id="x"><Strategy><Fixed month="July" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        string resolverDir = Path.Combine(Scratch, "imports");
        _ = Directory.CreateDirectory(resolverDir);
        File.WriteAllText(Path.Combine(resolverDir, "tool-shared.xml"), importedXml);
        string input = WriteScratchFile("lint-importing.xml", importingXml);

        (int exitCode, string output, _) = RunTool("lint", input, "--resolver-dir", resolverDir);

        Assert.AreEqual(CalendarTool.ExitSuccess, exitCode);
        Assert.IsTrue(output.Contains("OK", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that the info verb rejects a file that is not a pack with the failure code.
    /// </summary>
    [TestMethod]
    public void Run_WhenInfoTargetsNonPack_ShouldExitOne()
    {
        string path = WriteScratchFile("not-a-pack.bcal", "these are not the bytes you are looking for");

        (int exitCode, _, string error) = RunTool("info", path);

        Assert.AreEqual(CalendarTool.ExitFailure, exitCode);
        Assert.IsTrue(error.Length > 0, "The format failure must be reported.");
    }

    /// <summary>
    /// Verifies that an unsupported document extension is a usage error.
    /// </summary>
    [TestMethod]
    public void Run_WhenDocumentExtensionUnsupported_ShouldExitTwo()
    {
        string path = WriteScratchFile("wrong-extension.txt", ValidXml);

        (int exitCode, _, _) = RunTool("lint", path);

        Assert.AreEqual(CalendarTool.ExitUsage, exitCode);
    }
}
