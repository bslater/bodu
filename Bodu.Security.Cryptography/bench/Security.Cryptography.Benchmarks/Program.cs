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
    /// <summary>The name of the library's feature switch that sends every kernel to its scalar path.</summary>
    private const string DisableSimdSwitchName = "Bodu.Security.Cryptography.DisableSimd";

    /// <summary>
    /// Gets a value indicating whether the library's SIMD dispatch is switched off in this process.
    /// </summary>
    internal static bool IsSimdDisabled =>
        AppContext.TryGetSwitch(DisableSimdSwitchName, out bool disabled) && disabled;

    /// <summary>
    /// Runs the benchmarks selected by the supplied command-line arguments, the Argon2 harness when the first argument
    /// is <c>--argon2-harness</c>, or the cross-library throughput harness when it is <c>--crypto-harness</c>.
    /// </summary>
    /// <param name="args">Command-line arguments forwarded to the BenchmarkDotNet switcher or a harness.</param>
    /// <remarks>
    /// <c>--disable-simd</c>, anywhere in the arguments, sets the library's switch before any cryptography type is
    /// used, so its kernels give way to their scalar paths while the runtime and the BCL keep their vector code.
    /// </remarks>
    private static void Main(string[] args)
    {
        if (args.Contains("--disable-simd"))
        {
            AppContext.SetSwitch(DisableSimdSwitchName, true);
            args = [.. args.Where(arg => arg != "--disable-simd")];
        }

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
