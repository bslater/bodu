// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Running;

namespace Bodu.Security.Cryptography.Benchmarks;

/// <summary>
/// Provides the entry point for the cryptographic hash benchmark harness.
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
