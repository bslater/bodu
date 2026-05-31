// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Running;

namespace Bodu.Text.Configuration.Benchmarks;

/// <summary>
/// Provides the entry point for the configuration benchmark harness.
/// </summary>
internal sealed class Program
{
    /// <summary>
    /// Runs the benchmarks selected by the supplied command-line arguments.
    /// </summary>
    /// <param name="args">Command-line arguments forwarded to the BenchmarkDotNet switcher.</param>
    private static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
