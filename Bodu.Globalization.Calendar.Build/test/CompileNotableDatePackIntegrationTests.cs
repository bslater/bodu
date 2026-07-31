// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompileNotableDatePackIntegrationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Build;

/// <summary>
/// End-to-end tests driving <c>dotnet build</c> over a fixture project that consumes the package's
/// <c>.targets</c>: packs compile, rebuilds are incremental, edits recompile, and invalid documents fail the build.
/// </summary>
[TestClass]
public sealed class CompileNotableDatePackIntegrationTests
{
    /// <summary>A valid document the fixture compiles.</summary>
    private const string ValidXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="build.fixture">
          <NotableDates>
            <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday">
              <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>The scratch directory holding the fixture project, unique per run.</summary>
    private static readonly string Scratch = Path.Combine(Path.GetTempPath(), "bodu-calendar-build-tests-" + Guid.NewGuid().ToString("N"));

    /// <summary>The repository root, resolved from the test assembly location.</summary>
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

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
    /// Writes a fixture project consuming the repository's .targets with the task and tool paths pointed at the
    /// built outputs.
    /// </summary>
    /// <param name="projectDir">The fixture project directory.</param>
    /// <param name="documentXml">The rule document content.</param>
    private static void WriteFixtureProject(string projectDir, string documentXml)
    {
        _ = Directory.CreateDirectory(Path.Combine(projectDir, "rules"));
        File.WriteAllText(Path.Combine(projectDir, "rules", "holidays.xml"), documentXml);

        string configuration = AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ? "Release" : "Debug";
        string taskAssembly = Path.Combine(RepoRoot, "Bodu.Globalization.Calendar.Build", "src", "bin", configuration, "netstandard2.0", "Bodu.Globalization.Calendar.Build.dll");
        string toolDll = Path.Combine(RepoRoot, "Bodu.Globalization.Calendar.Tool", "src", "bin", configuration, "net8.0", "Bodu.Globalization.Calendar.Tool.dll");
        string targets = Path.Combine(RepoRoot, "Bodu.Globalization.Calendar.Build", "build", "Bodu.Globalization.Calendar.Build.targets");

        File.WriteAllText(Path.Combine(projectDir, "Fixture.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <BoduCalendarTaskAssembly>{taskAssembly}</BoduCalendarTaskAssembly>
                <BoduCalendarToolDll>{toolDll}</BoduCalendarToolDll>
              </PropertyGroup>
              <ItemGroup>
                <NotableDatePack Include="rules\holidays.xml" />
              </ItemGroup>
              <Import Project="{targets}" />
            </Project>
            """);
    }

    /// <summary>
    /// Runs <c>dotnet build</c> in the fixture directory.
    /// </summary>
    /// <param name="projectDir">The fixture project directory.</param>
    /// <returns>The exit code and combined output.</returns>
    private static (int ExitCode, string Output) DotnetBuild(string projectDir)
    {
        ProcessStartInfo start = new("dotnet", "build -v m --nologo -nodeReuse:false")
        {
            WorkingDirectory = projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.Environment["DOTNET_ROLL_FORWARD"] = "LatestMajor";

        using Process process = Process.Start(start)!;
        string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, output);
    }

    /// <summary>
    /// Verifies the full lifecycle: the first build compiles a loadable pack into the output directory, a second
    /// build skips the up-to-date target, and touching the document recompiles.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Build_WhenFixtureConsumesTargets_ShouldCompileIncrementally()
    {
        string projectDir = Path.Combine(Scratch, "lifecycle");
        WriteFixtureProject(projectDir, ValidXml);

        (int firstExit, string firstOutput) = DotnetBuild(projectDir);
        Assert.AreEqual(0, firstExit, $"First build failed:\n{firstOutput}");

        string pack = Path.Combine(projectDir, "bin", "Debug", "net8.0", "holidays.bcal");
        Assert.IsTrue(File.Exists(pack), $"Pack not produced:\n{firstOutput}");

        using (FileStream stream = File.OpenRead(pack))
        {
            NotableDateResource resource = NotableDateResourceLoader.LoadBinary(stream);
            Assert.AreEqual("build.fixture", resource.ResourceId);
        }

        // Second build: the compile target's (input, output) pair is up to date, so the pack is not rewritten.
        DateTime compiledAt = File.GetLastWriteTimeUtc(pack);
        (int secondExit, string secondOutput) = DotnetBuild(projectDir);
        Assert.AreEqual(0, secondExit, $"Second build failed:\n{secondOutput}");
        Assert.AreEqual(compiledAt, File.GetLastWriteTimeUtc(pack), "An up-to-date pack must not be recompiled.");

        // Touching the document invalidates the pair and recompiles.
        File.SetLastWriteTimeUtc(Path.Combine(projectDir, "rules", "holidays.xml"), DateTime.UtcNow);
        (int thirdExit, string thirdOutput) = DotnetBuild(projectDir);
        Assert.AreEqual(0, thirdExit, $"Rebuild failed:\n{thirdOutput}");
        Assert.IsTrue(File.GetLastWriteTimeUtc(pack) > compiledAt, "An edited document must recompile its pack.");
    }

    /// <summary>
    /// Verifies that an invalid document fails the build with the tool's stable diagnostic code in the log.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Build_WhenDocumentIsInvalid_ShouldFailWithDiagnosticCode()
    {
        const string invalidXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="build.invalid">
              <NotableDates>
                <NotableDate id="mystery-day" displayName="Mystery Day" category="Observance">
                  <Rules><Rule id="x"><Strategy><Algorithm key="no-such-algorithm" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        string projectDir = Path.Combine(Scratch, "invalid");
        WriteFixtureProject(projectDir, invalidXml);

        (int exitCode, string output) = DotnetBuild(projectDir);

        Assert.AreNotEqual(0, exitCode, "An invalid document must fail the build.");
        Assert.IsTrue(output.Contains("BODU-CAL-ALGORITHM", StringComparison.Ordinal), $"The diagnostic code must surface in the build log:\n{output}");
    }
}
