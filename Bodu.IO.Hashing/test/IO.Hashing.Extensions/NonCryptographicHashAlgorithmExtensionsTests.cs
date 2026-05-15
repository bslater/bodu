// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// Tests covering the <see cref="NonCryptographicHashAlgorithmExtensions" /> surface — including
/// <c>AppendData</c>, <c>AppendDataAsync</c>, <c>VerifyHash</c>, <c>VerifyHashAsync</c>,
/// <c>TryVerifyHash</c>, and <c>TryVerifyHashAsync</c>. Tests for each method live in their own partial file.
/// </summary>
/// <remarks>
/// <para>
/// Tests use <see cref="MonitoringNonCryptographicHashAlgorithm" />, a deterministic test-infrastructure algorithm
/// that accumulates input bytes as a 32-bit unsigned integer sum. This makes the expected hash of any input
/// trivially computable — <c>BitConverter.GetBytes((uint)sum)</c> — which is why the fixtures below use small byte
/// arrays rather than published test vectors.
/// </para>
/// <para>
/// This file holds only shared fixtures and helpers. All test methods are declared in per-method partial files.
/// </para>
/// </remarks>
[TestClass]
public partial class NonCryptographicHashAlgorithmExtensionsTests
{

    /// <summary>Deterministic sample input whose additive byte sum is 10.</summary>
    private static readonly byte[] s_sampleData = [1, 2, 3, 4];

    /// <summary>The encoding paired with <see cref="s_sampleString" /> and <see cref="s_sampleStringHash" />.</summary>
    private static readonly Encoding s_sampleEncoding = Encoding.ASCII;

    /// <summary>Expected hash of <see cref="s_sampleData" /> (byte sum = 10, platform byte order uint).</summary>
    private static readonly byte[] s_sampleHash = BitConverter.GetBytes((uint)(1 + 2 + 3 + 4));

    /// <summary>Uppercase hex representation of <see cref="s_sampleHash" />.</summary>
    private static readonly string s_sampleHex = Convert.ToHexString(s_sampleHash);

    /// <summary>Sample ASCII string whose encoded byte sum is 394 (<c>'a'</c>+<c>'b'</c>+<c>'c'</c>+<c>'d'</c>).</summary>
    private static readonly string s_sampleString = "abcd";

    /// <summary>Expected hash of <see cref="s_sampleString" /> encoded as ASCII.</summary>
    private static readonly byte[] s_sampleStringHash = BitConverter.GetBytes((uint)(97 + 98 + 99 + 100));

    /// <summary>
    /// Creates a fresh <see cref="MonitoringNonCryptographicHashAlgorithm" /> instance for a single test.
    /// </summary>
    /// <returns>A new <see cref="MonitoringNonCryptographicHashAlgorithm" /> with no accumulated state.</returns>
    private static MonitoringNonCryptographicHashAlgorithm CreateAlgorithm() => new();

}
