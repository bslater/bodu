// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryEncodingsTests.TryDecodeFailurePolicy.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Pins the cross-codec <see cref="IBinaryEncoding.TryDecode" /> failure policy: on a malformed input every
/// implementation must set <c>bytesWritten</c> to zero, matching the BCL convention used by
/// <c>Convert.TryFromBase64Chars</c> and friends.
/// </summary>
[TestClass]
public sealed class BinaryEncodingsTests_TryDecodeFailurePolicy
{
    /// <summary>
    /// Returns the binary-encoding adapter under test paired with an input string that the adapter rejects.
    /// Each adapter has its own malformed string because the supported alphabets overlap substantially.
    /// </summary>
    /// <returns>Pairs of <see cref="IBinaryEncoding" /> and malformed character input.</returns>
    public static IEnumerable<object[]> MalformedInputs()
    {
        // A pound sign is outside Base16/32/58/64 alphabets but lives inside Ascii85's '!'..'u' range, so the
        // two Base85 variants need different rejection inputs. '~' is outside both Z85 and Ascii85 alphabets.
        const string outsideBaseAlphabets = "####";

        yield return new object[] { BinaryEncodings.Base16Lower, outsideBaseAlphabets };
        yield return new object[] { BinaryEncodings.Base16Upper, outsideBaseAlphabets };
        yield return new object[] { BinaryEncodings.Base32, outsideBaseAlphabets };
        yield return new object[] { BinaryEncodings.Base32Crockford, outsideBaseAlphabets };
        yield return new object[] { BinaryEncodings.Base32Hex, outsideBaseAlphabets };
        yield return new object[] { BinaryEncodings.Base32ZBase32, outsideBaseAlphabets };
        yield return new object[] { BinaryEncodings.Base58, outsideBaseAlphabets };
        yield return new object[] { BinaryEncodings.Base58Ripple, outsideBaseAlphabets };
        yield return new object[] { BinaryEncodings.Base64, outsideBaseAlphabets };
        yield return new object[] { BinaryEncodings.Base64Mime, outsideBaseAlphabets };
        yield return new object[] { BinaryEncodings.Base64UrlSafe, outsideBaseAlphabets };
        yield return new object[] { BinaryEncodings.Ascii85, "~~~~~" };
        yield return new object[] { BinaryEncodings.Z85, "~~~~~" };
    }

    /// <summary>
    /// Verifies that every <see cref="IBinaryEncoding" /> adapter returns <see langword="false" /> and sets
    /// <c>bytesWritten</c> to zero when handed malformed input that the adapter cannot decode. The destination
    /// contents are intentionally unspecified — this test pins the <c>bytesWritten</c> contract only.
    /// </summary>
    /// <param name="encoding">The encoding under test.</param>
    /// <param name="malformed">An input string that the encoding rejects.</param>
    [TestMethod]
    [DynamicData(nameof(MalformedInputs))]
    public void TryDecode_WhenInputIsMalformed_ShouldReturnFalseAndZeroBytesWritten(
        IBinaryEncoding encoding,
        string malformed)
    {
        ArgumentNullException.ThrowIfNull(encoding);

        Span<byte> destination = stackalloc byte[encoding.GetMaxDecodedLength(malformed.Length)];

        var success = encoding.TryDecode(malformed.AsSpan(), destination, out var bytesWritten);

        Assert.IsFalse(success);
        Assert.AreEqual(0, bytesWritten);
    }
}
