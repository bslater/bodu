// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85GitTests.Length.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base85GitTests
{
    /// <summary>
    /// Verifies that <see cref="Base85.GetEncodedLength(ReadOnlySpan{byte}, Base85Variant, BaseFormattingOptions)" />
    /// equals the actual characters written by the encoder for Git compact mode, including all-zero groups (which are
    /// not collapsed to a shortcut).
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(64)]
    public void GetEncodedLength_ForGit_ShouldEqualEncodedOutput(int byteCount)
    {
        byte[] zeros = new byte[byteCount];

        int reported = Base85.GetEncodedLength(zeros, Base85Variant.Git);
        string encoded = Base85.Encode(zeros, Base85Variant.Git);

        Assert.AreEqual(encoded.Length, reported);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.GetEncodedLength(ReadOnlySpan{byte}, Base85Variant, BaseFormattingOptions)" />
    /// equals the <c>charsWritten</c> reported by <see cref="Base85.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, Base85Variant, BaseFormattingOptions)" />
    /// for non-trivial Git data.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_ForGit_ShouldEqualTryEncodeCharsWritten()
    {
        byte[] bytes = Ascii("hello world");
        int reported = Base85.GetEncodedLength(bytes, Base85Variant.Git);

        Span<char> destination = new char[Base85.GetMaxEncodedLength(bytes.Length, Base85Variant.Git)];
        Base85.TryEncode(bytes, destination, out int charsWritten, Base85Variant.Git);

        Assert.AreEqual(charsWritten, reported);
    }
}
