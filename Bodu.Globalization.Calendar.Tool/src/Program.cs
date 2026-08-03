// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Tool;

/// <summary>
/// Console host for the <c>bodu-calendar</c> tool.
/// </summary>
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
