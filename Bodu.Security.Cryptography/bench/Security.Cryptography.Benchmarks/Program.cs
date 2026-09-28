// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// Runs the benchmarks selected by the supplied command-line arguments, the Argon2 harness when the first argument
    /// is <c>--argon2-harness</c>, or the cross-library throughput harness when it is <c>--crypto-harness</c>.
    /// </summary>
    /// <param name="args">Command-line arguments forwarded to the BenchmarkDotNet switcher or a harness.</param>
    private static void Main(string[] args)
    {
        if (args is ["--argon2-harness", .. var harnessArgs])
        {
            Argon2Harness.Run(harnessArgs);
            return;
        }

        if (args is ["--crypto-harness", .. var cryptoArgs])
        {
            CryptoHarness.Run(cryptoArgs);
            return;
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
