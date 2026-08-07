// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarToolTests.Arguments.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Tool;

/// <summary>
/// Verifies the argument parser shared by every verb.
/// </summary>
/// <remarks>
/// The parser is the tool's whole command-line contract, and each rejection carries a different message. Every verb
/// routes through it, so the cases are exercised once here rather than repeated per verb - with one case per verb to
/// prove the routing, since a verb that forgets to check the parse result would run on an empty input path.
/// </remarks>
public sealed partial class CalendarToolTests
{
    /// <summary>
    /// Verifies that an argument vector the parser rejects exits with the usage code and prints the usage help.
    /// </summary>
    /// <param name="args">The argument vector.</param>
    [TestMethod]
    [DataRow(new[] { "lint" }, DisplayName = "verb with no input file")]
    [DataRow(new[] { "compile" }, DisplayName = "compile with no input file")]
    [DataRow(new[] { "info" }, DisplayName = "info with no input file")]
    [DataRow(new[] { "lint", "doc.xml", "-o" }, DisplayName = "-o with no value")]
    [DataRow(new[] { "lint", "doc.xml", "--output" }, DisplayName = "--output with no value")]
    [DataRow(new[] { "lint", "doc.xml", "--resolver-dir" }, DisplayName = "--resolver-dir with no value")]
    [DataRow(new[] { "lint", "doc.xml", "--bogus" }, DisplayName = "unknown option")]
    [DataRow(new[] { "lint", "first.xml", "second.xml" }, DisplayName = "second positional argument")]
    public void Run_WhenArgumentsAreInvalid_ShouldPrintUsageAndExitTwo(string[] args)
    {
        (int exitCode, string output, string error) = RunTool(args);

        Assert.AreEqual(CalendarTool.ExitUsage, exitCode);
        Assert.IsNotEmpty(error);
        Assert.IsEmpty(output);
    }

    /// <summary>
    /// Verifies that an option value that merely looks like another option is still taken as the value, so a path
    /// beginning with a dash is not silently swallowed by the parser.
    /// </summary>
    [TestMethod]
    public void Run_WhenOptionValueLooksLikeAnOption_ShouldBeConsumedAsTheValue()
    {
        string path = WriteScratchFile("dash-value.xml", ValidXml);

        (int exitCode, _, string error) = RunTool("lint", path, "--resolver-dir", "--not-a-directory");

        // The value is consumed rather than reported as an unknown option, so this is not a usage failure.
        Assert.AreNotEqual(CalendarTool.ExitUsage, exitCode);
        Assert.IsEmpty(error);
    }

    /// <summary>
    /// Verifies that <c>info</c> on a path that does not exist reports a failure rather than a usage error, since the
    /// arguments themselves were well formed.
    /// </summary>
    [TestMethod]
    public void Run_WhenInfoTargetsMissingFile_ShouldExitOne()
    {
        (int exitCode, _, string error) = RunTool("info", Path.Combine(Scratch, "no-such-pack.bcal"));

        Assert.AreEqual(CalendarTool.ExitFailure, exitCode);
        Assert.IsNotEmpty(error);
    }
}
