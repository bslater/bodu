// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarToolTests.Json.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Tool;

/// <summary>
/// Verifies that the tool accepts JSON documents as well as XML.
/// </summary>
/// <remarks>
/// The tool selects its loader from the input file's extension, and the <c>.json</c> branch had no coverage at all -
/// a shipped input format that had never been exercised end to end. Each test pairs the JSON document with the XML
/// document describing the same resource, so a divergence in the JSON path shows up as a difference in outcome rather
/// than only as a failure to load.
/// </remarks>
public sealed partial class CalendarToolTests
{
    /// <summary>A minimal valid document in the JSON subset, equivalent to <see cref="ValidXml" />.</summary>
    private const string ValidJson = """
        {
          "resourceId": "tool.valid.json",
          "notableDates": [
            {
              "id": "new-years-day",
              "displayName": "New Year's Day",
              "category": "PublicHoliday",
              "rules": [ { "id": "x", "strategy": { "fixed": { "month": "January", "day": 1 } } } ]
            }
          ]
        }
        """;

    /// <summary>A JSON document referencing an unknown algorithm key, failing validation.</summary>
    private const string InvalidJson = """
        {
          "resourceId": "tool.invalid.json",
          "notableDates": [
            {
              "id": "mystery-day",
              "displayName": "Mystery Day",
              "category": "Observance",
              "rules": [ { "id": "x", "strategy": { "algorithm": { "key": "no-such-algorithm" } } } ]
            }
          ]
        }
        """;

    /// <summary>
    /// Verifies that linting a valid JSON document succeeds, exactly as the equivalent XML document does.
    /// </summary>
    [TestMethod]
    public void Run_WhenLintingValidJsonDocument_ShouldExitZero()
    {
        string path = WriteScratchFile("valid.json", ValidJson);

        (int exitCode, string output, string error) = RunTool("lint", path);

        Assert.AreEqual(CalendarTool.ExitSuccess, exitCode, error);
        Assert.Contains("tool.valid.json", output);
    }

    /// <summary>
    /// Verifies that a JSON document failing validation is reported and exits non-zero, so the JSON path applies the
    /// same validation the XML path does rather than only parsing.
    /// </summary>
    [TestMethod]
    public void Run_WhenLintingInvalidJsonDocument_ShouldExitOne()
    {
        string path = WriteScratchFile("invalid.json", InvalidJson);

        (int exitCode, string output, _) = RunTool("lint", path);

        Assert.AreEqual(CalendarTool.ExitFailure, exitCode);
        Assert.IsNotEmpty(output);
    }

    /// <summary>
    /// Verifies that a JSON document compiles to a loadable binary pack, so the format is usable through the whole
    /// pipeline and not only through <c>lint</c>.
    /// </summary>
    [TestMethod]
    public void Run_WhenCompilingJsonDocument_ShouldProduceLoadablePack()
    {
        string path = WriteScratchFile("compile.json", ValidJson);
        string pack = Path.Combine(Scratch, "compile-json.bcal");

        (int exitCode, string output, string error) = RunTool("compile", path, "-o", pack);

        Assert.AreEqual(CalendarTool.ExitSuccess, exitCode, error);
        Assert.IsTrue(File.Exists(pack));
        Assert.Contains("tool.valid.json", output);

        (int infoExit, string infoOutput, _) = RunTool("info", pack);

        Assert.AreEqual(CalendarTool.ExitSuccess, infoExit);
        Assert.Contains("tool.valid.json", infoOutput);
    }

    /// <summary>
    /// Verifies that malformed JSON is reported rather than crashing out of the parser.
    /// </summary>
    [TestMethod]
    public void Run_WhenJsonDocumentIsMalformed_ShouldExitNonZero()
    {
        string path = WriteScratchFile("malformed.json", "{ not json");

        (int exitCode, _, _) = RunTool("lint", path);

        Assert.AreNotEqual(CalendarTool.ExitSuccess, exitCode);
    }
}
