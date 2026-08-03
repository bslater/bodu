// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarToolTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Tool;

/// <summary>
/// In-process tests for the <c>bodu-calendar</c> command surface: verb routing, exit codes, diagnostics output, and
/// the compile/info pipeline over real documents.
/// </summary>
[TestClass]
public sealed partial class CalendarToolTests
{
    /// <summary>A minimal valid document for the verb tests.</summary>
    private const string ValidXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="tool.valid">
          <NotableDates>
            <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday">
              <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>A document referencing an unknown algorithm key, failing validation.</summary>
    private const string InvalidXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="tool.invalid">
          <NotableDates>
            <NotableDate id="mystery-day" displayName="Mystery Day" category="Observance">
              <Rules><Rule id="x"><Strategy><Algorithm key="no-such-algorithm" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>The scratch directory for this test class's files, unique per run.</summary>
    private static readonly string Scratch = Path.Combine(Path.GetTempPath(), "bodu-calendar-tool-tests-" + Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Creates the scratch directory.
    /// </summary>
    /// <param name="context">The test context.</param>
    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _ = context;
        _ = Directory.CreateDirectory(Scratch);
    }

    /// <summary>
    /// Removes the scratch directory.
    /// </summary>
    [ClassCleanup]
    public static void ClassCleanup()
    {
        if (Directory.Exists(Scratch))
            Directory.Delete(Scratch, recursive: true);
    }

    /// <summary>
    /// Writes content to a scratch file and returns its path.
    /// </summary>
    /// <param name="fileName">The file name within the scratch directory.</param>
    /// <param name="content">The content to write.</param>
    /// <returns>The file path.</returns>
    private static string WriteScratchFile(string fileName, string content)
    {
        string path = Path.Combine(Scratch, fileName);
        File.WriteAllText(path, content);

        return path;
    }

    /// <summary>
    /// Runs the tool capturing output and error text.
    /// </summary>
    /// <param name="args">The argument vector.</param>
    /// <returns>The exit code with the captured output and error text.</returns>
    private static (int ExitCode, string Output, string Error) RunTool(params string[] args)
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = CalendarTool.Run(args, output, error);

        return (exitCode, output.ToString(), error.ToString());
    }
}
