// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Running;

namespace Bodu.Collections.Generic.Concurrent.Benchmarks;

/// <summary>
/// Entry point for the <see cref="ConcurrentHashSet{T}" /> benchmark suite.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Discovers and runs the benchmarks declared in this assembly.
    /// </summary>
    /// <param name="args">
    /// BenchmarkDotNet command-line switches — for example <c>--filter *</c> to run every benchmark, or
    /// <c>--list flat</c> to enumerate them without running.
    /// </param>
    private static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
