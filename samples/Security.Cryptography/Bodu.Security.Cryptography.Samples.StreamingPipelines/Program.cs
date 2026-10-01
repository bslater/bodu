// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Samples.StreamingPipelines.Scenarios;

namespace Bodu.Security.Cryptography.Samples.StreamingPipelines;

/// <summary>
/// Entry point for the streaming-pipelines sample: the <c>Bodu.Security.Cryptography.Extensions</c> surface used the
/// way an application uses it - encrypting and decrypting streams synchronously and asynchronously, running the same
/// extensions over the BCL's own <c>Aes</c> and <c>SHA256</c>, verifying downloaded content against published digests,
/// and composing all of it into an encrypt-then-MAC sealed file. Every key, IV, nonce, and payload is fixed, so all
/// output is deterministic.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    /// <returns>A task that completes when every scenario has finished.</returns>
    public static async Task Main()
    {
        Console.WriteLine("Bodu.Security.Cryptography.Samples.StreamingPipelines");
        Console.WriteLine("=====================================================");
        Console.WriteLine();

        EncryptingStreams.Run();
        await AsyncAndBclInterop.RunAsync().ConfigureAwait(false);
        await VerifyingDownloads.RunAsync().ConfigureAwait(false);
        await SealedFilePipeline.RunAsync().ConfigureAwait(false);

        Console.WriteLine("Done.");
    }
}
