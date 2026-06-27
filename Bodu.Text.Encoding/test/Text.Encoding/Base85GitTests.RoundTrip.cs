// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85GitTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base85GitTests
{
    /// <summary>
    /// Verifies that Git compact encode followed by decode recovers the original bytes across input sizes spanning
    /// all four residue classes mod 4.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(6)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(15)]
    [DataRow(16)]
    [DataRow(33)]
    public void RoundTrip_ForGitAcrossLengths_ShouldRecoverOriginalBytes(int byteCount)
    {
        byte[] original = new byte[byteCount];
        for (int i = 0; i < byteCount; i++)
            original[i] = (byte)((i * 17) + 3);

        string encoded = Base85.Encode(original, Base85Variant.Git);
        byte[] decoded = Base85.Decode(encoded, Base85Variant.Git);

        CollectionAssert.AreEqual(original, decoded, $"Git round trip failed for length={byteCount}.");
    }

    /// <summary>
    /// Verifies that every Git compact round-trip succeeds for input lengths 0 through 1024. Tagged Regression
    /// because of the exhaustive length sweep.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void RoundTrip_ForGitLengthsZeroThroughOneThousandTwentyFour_ShouldRecover()
    {
        var random = new Random(0x6917);
        for (int length = 0; length <= 1024; length++)
        {
            byte[] original = new byte[length];
            random.NextBytes(original);

            string encoded = Base85.Encode(original, Base85Variant.Git);
            byte[] decoded = Base85.Decode(encoded, Base85Variant.Git);

            CollectionAssert.AreEqual(original, decoded, $"Git round trip failed for length={length}.");
        }
    }

    /// <summary>
    /// Verifies that the span-based TryEncode + TryDecode round-trip recovers the original bytes for Git.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenSpanTryPathForGit_ShouldRecoverOriginal()
    {
        byte[] original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE };
        char[] charBuffer = new char[Base85.GetMaxEncodedLength(original.Length, Base85Variant.Git)];
        byte[] byteBuffer = new byte[Base85.GetMaxDecodedLength(charBuffer.Length)];

        bool encOk = Base85.TryEncode(original.AsSpan(), charBuffer, out int charsWritten, Base85Variant.Git);
        bool decOk = Base85.TryDecode(charBuffer.AsSpan(0, charsWritten), byteBuffer, out int bytesWritten, Base85Variant.Git);

        Assert.IsTrue(encOk);
        Assert.IsTrue(decOk);
        Assert.AreEqual(original.Length, bytesWritten);
        CollectionAssert.AreEqual(original, byteBuffer.AsSpan(0, bytesWritten).ToArray());
    }
}
