// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Globalization.Calendar.Tool;

/// <summary>
/// Console host for the <c>bodu-calendar</c> tool.
/// </summary>
/// <remarks>
/// Excluded from coverage: the host exists only to bind the process entry point to the console streams, and every
/// behavior it could contribute lives in <see cref="CalendarTool.Run(string[], TextWriter, TextWriter)" />, which the
/// suite drives directly. Reaching this line would mean spawning the process, which tests the runtime's argument
/// marshalling rather than anything this repository owns.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static class Program
{
    /// <summary>
    /// Delegates to the shared in-process entry point.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The process exit code.</returns>
    private static int Main(string[] args) =>
        CalendarTool.Run(args, Console.Out, Console.Error);
}
