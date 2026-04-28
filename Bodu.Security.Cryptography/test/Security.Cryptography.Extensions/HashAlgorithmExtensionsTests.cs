// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Tests covering the <see cref="HashAlgorithmExtensions" /> surface — including
/// <c>AppendData</c>, <c>VerifyHash</c>, <c>VerifyHashAsync</c>, <c>TryVerifyHash</c>, and
/// <c>TryVerifyHashAsync</c>. Tests for each method live in their own partial file.
/// </summary>
/// <remarks>
/// <para>
/// Tests use <see cref="MonitoringHashAlgorithm" />, a deterministic test-infrastructure algorithm
/// that accumulates input bytes as a 32-bit unsigned integer sum. This makes the expected hash of
/// any input trivially computable — <c>BitConverter.GetBytes((uint)sum)</c> — which is why the
/// fixtures below use small byte arrays rather than cryptographic test vectors.
/// </para>
/// <para>
/// This file holds only shared fixtures and helpers. All test methods are declared in per-method
/// partial files (for example <c>HashAlgorithmExtensionsTests_VerifyHash.cs</c>).
/// </para>
/// </remarks>
[TestClass]
public partial class HashAlgorithmExtensionsTests
{
    /// <summary>Deterministic sample input whose additive byte sum is 10.</summary>
    private static readonly byte[] SampleData = { 1, 2, 3, 4 };

    /// <summary>Expected hash of <see cref="SampleData" /> (byte sum = 10, little-endian uint).</summary>
    private static readonly byte[] SampleHash = BitConverter.GetBytes((uint)(1 + 2 + 3 + 4));

    /// <summary>Uppercase hex representation of <see cref="SampleHash" />.</summary>
    private static readonly string SampleHex = Convert.ToHexString(SampleHash);

    /// <summary>Sample ASCII string whose encoded byte sum is 394 (<c>'a'</c>+<c>'b'</c>+<c>'c'</c>+<c>'d'</c>).</summary>
    private static readonly string SampleString = "abcd";

    /// <summary>Expected hash of <see cref="SampleString" /> encoded as ASCII.</summary>
    private static readonly byte[] SampleStringHash = BitConverter.GetBytes((uint)(97 + 98 + 99 + 100));

    /// <summary>The encoding paired with <see cref="SampleString" /> and <see cref="SampleStringHash" />.</summary>
    private static readonly Encoding SampleEncoding = Encoding.ASCII;

    /// <summary>
    /// Creates a fresh <see cref="MonitoringHashAlgorithm" /> instance for a single test.
    /// </summary>
    /// <returns>A new <see cref="MonitoringHashAlgorithm" /> with no accumulated state.</returns>
    private static MonitoringHashAlgorithm CreateAlgorithm() => new();
}
