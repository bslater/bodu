// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompileNotableDatePack.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bodu.Globalization.Calendar.Build;

/// <summary>
/// Compiles one notable-date XML/JSON document to a sealed <c>.bcal</c> binary pack by invoking the
/// <c>bodu-calendar</c> tool through the <c>dotnet</c> host.
/// </summary>
/// <remarks>
/// <para>
/// The task is a thin <see cref="ToolTask" /> over <c>bodu-calendar compile</c>, so the build-time compiler and the
/// command-line tool share one code path. Validation failures surface as build errors carrying the tool's stable
/// <c>BODU-CAL-*</c> diagnostic lines; the accompanying <c>.targets</c> file wires <c>NotableDatePack</c> items to this
/// task with input/output incrementality.
/// </para>
/// </remarks>
public sealed class CompileNotableDatePack
    : ToolTask
{
    /// <summary>
    /// Gets or sets the notable-date document to compile (<c>.xml</c> or <c>.json</c>).
    /// </summary>
    [Required]
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pack file path the compiler writes.
    /// </summary>
    [Required]
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path of the <c>bodu-calendar</c> tool assembly executed through the <c>dotnet</c> host.
    /// </summary>
    [Required]
    public string ToolDll { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional directory whose files satisfy document imports.
    /// </summary>
    public string? ResolverDir { get; set; }

    /// <summary>
    /// Gets the tool executable name: the <c>dotnet</c> host.
    /// </summary>
    protected override string ToolName =>
        "dotnet";

    /// <summary>
    /// Gets the importance of the tool's standard output: high, so the compile summary and diagnostics surface even at
    /// minimal build verbosity.
    /// </summary>
    protected override MessageImportance StandardOutputLoggingImportance =>
        MessageImportance.High;

    /// <summary>
    /// Gets the importance of the tool's standard error: high, so the stable <c>BODU-CAL-*</c> diagnostic lines surface
    /// in the build log alongside the failure.
    /// </summary>
    protected override MessageImportance StandardErrorLoggingImportance =>
        MessageImportance.High;

    /// <summary>
    /// Resolves the tool path; the <c>dotnet</c> host is expected on the PATH.
    /// </summary>
    /// <returns>The tool name, resolved by the process launcher via the PATH.</returns>
    protected override string GenerateFullPathToTool() =>
        ToolName;

    /// <summary>
    /// Ensures the output directory exists, then runs the tool.
    /// </summary>
    /// <returns><see langword="true" /> when the pack compiled.</returns>
    public override bool Execute()
    {
        string? outputDirectory = Path.GetDirectoryName(Path.GetFullPath(Output));
        if (!string.IsNullOrEmpty(outputDirectory))
            _ = Directory.CreateDirectory(outputDirectory);

        return base.Execute();
    }

    /// <summary>
    /// Builds the <c>dotnet exec</c> command line for the compile verb.
    /// </summary>
    /// <returns>The quoted command line.</returns>
    protected override string GenerateCommandLineCommands()
    {
        CommandLineBuilder builder = new();
        builder.AppendSwitch("exec");

        // The tool targets the calendar packages' baseline framework; roll forward across majors explicitly so the
        // task works on any newer installed runtime regardless of the build node's inherited environment.
        builder.AppendSwitch("--roll-forward");
        builder.AppendSwitch("Major");
        builder.AppendFileNameIfNotNull(ToolDll);
        builder.AppendSwitch("compile");
        builder.AppendFileNameIfNotNull(Input);
        builder.AppendSwitch("-o");
        builder.AppendFileNameIfNotNull(Output);

        if (!string.IsNullOrEmpty(ResolverDir))
        {
            builder.AppendSwitch("--resolver-dir");
            builder.AppendFileNameIfNotNull(ResolverDir);
        }

        return builder.ToString();
    }
}
