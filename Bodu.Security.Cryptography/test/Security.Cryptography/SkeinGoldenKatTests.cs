// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinGoldenKatTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Drives <see cref="Skein256" />, <see cref="Skein512" />, and <see cref="Skein1024" /> against the Skein 1.3
/// reference golden known-answer test file, loaded dynamically from the embedded resource.
/// </summary>
[TestClass]
public sealed class SkeinGoldenKatTests
{
    /// <summary>Resource name of the embedded Skein 1.3 golden-KAT (short) reference file.</summary>
    private const string KatResourceName = "Bodu.Security.Cryptography.Skein.skein_golden_kat_short.txt";

    /// <summary>Citation propagated into each emitted vector for diagnostic output on failure.</summary>
    private const string KatSource = "Skein 1.3 golden KAT";

    /// <summary>The output sizes each Skein state variant supports, used to skip records Bodu cannot produce.</summary>
    private static readonly Dictionary<int, int[]> PermittedOutputs = new()
    {
        [256] = [128, 160, 224, 256],
        [512] = [128, 160, 224, 256, 384, 512],
        [1024] = [384, 512, 1024],
    };

    /// <summary>
    /// Loads every plain-hash Skein golden-KAT vector whose output size the corresponding Bodu variant supports.
    /// </summary>
    /// <returns>One row per supported vector; each row contains a single <see cref="SkeinKnownAnswer" />.</returns>
    /// <exception cref="InvalidOperationException">The embedded KAT resource cannot be located.</exception>
    private static IEnumerable<object[]> SkeinGoldenVectors()
    {
        using Stream stream = typeof(SkeinGoldenKatTests).Assembly.GetManifestResourceStream(KatResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{KatResourceName}' is not present in the test assembly. " +
                "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");

        foreach (SkeinKnownAnswer vector in SkeinGoldenKatReader.Read(stream, KatSource))
        {
            if (PermittedOutputs.TryGetValue(vector.StateSizeBits, out int[]? sizes) && Array.IndexOf(sizes, vector.OutputBits) >= 0)
                yield return new object[] { vector };
        }
    }

    /// <summary>
    /// Produces a human-readable display name for a KAT row so failures trace back to the reference record.
    /// </summary>
    /// <param name="methodInfo">The test method's reflection info (provided by the test runner).</param>
    /// <param name="data">The row data (a single <see cref="SkeinKnownAnswer" />).</param>
    /// <returns>A short label identifying this vector.</returns>
    public static string GetSkeinVectorDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) =>
        data[0] is SkeinKnownAnswer v ? v.Name : methodInfo.Name;

    /// <summary>
    /// Verifies that the Skein variant for the record's state size, producing the record's output size, reproduces the
    /// exact digest from the Skein 1.3 reference golden-KAT file for the given message.
    /// </summary>
    /// <param name="vector">The Skein golden-KAT vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(SkeinGoldenVectors), DynamicDataDisplayName = nameof(GetSkeinVectorDisplayName))]
    public void ComputeHash_WithSkeinGoldenKatVector_ShouldMatchReferenceDigest(SkeinKnownAnswer vector)
    {
        using HashAlgorithm skein = vector.StateSizeBits switch
        {
            256 => new Skein256(vector.OutputBits),
            512 => new Skein512(vector.OutputBits),
            1024 => new Skein1024(vector.OutputBits),
            _ => throw new InvalidOperationException($"Unexpected Skein state size {vector.StateSizeBits}."),
        };

        byte[] actual = skein.ComputeHash(vector.Message);

        CollectionAssert.AreEqual(vector.Digest, actual,
            $"{vector.Name}: must match the Skein 1.3 reference digest.");
    }
}
