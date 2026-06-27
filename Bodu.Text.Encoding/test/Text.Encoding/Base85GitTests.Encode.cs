// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85GitTests.Encode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Encoding;

public sealed partial class Base85GitTests
{
    /// <summary>
    /// Verifies that <see cref="Base85.Encode(ReadOnlySpan{byte}, Base85Variant, BaseFormattingOptions)" /> reproduces
    /// every Git compact-mode Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(Base85GitKnownAnswerVectors.GitCompactVectors), typeof(Base85GitKnownAnswerVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Encode_ForGitCompactKnownAnswerVector_ShouldMatch(EncodingKnownAnswerVector vector)
    {
        string actual = Base85.Encode(vector.DecodedBytes, Base85Variant.Git);

        Assert.AreEqual(vector.Encoded, actual);
    }

    /// <summary>
    /// Verifies that Git compact mode does not collapse an all-zero group to a shortcut, emitting five
    /// alphabet characters instead.
    /// </summary>
    [TestMethod]
    public void Encode_WhenAllZeroGroupForGit_ShouldNotEmitShortcut()
    {
        string actual = Base85.Encode(new byte[] { 0x00, 0x00, 0x00, 0x00 }, Base85Variant.Git);

        Assert.AreEqual("00000", actual);
    }

    /// <summary>
    /// Verifies that Git compact mode emits two, three, or four characters for a final remainder of one, two,
    /// or three bytes respectively.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <param name="expectedLength">The expected encoded character count.</param>
    [TestMethod]
    [DataRow(1, 2)]
    [DataRow(2, 3)]
    [DataRow(3, 4)]
    [DataRow(4, 5)]
    [DataRow(5, 7)]
    [DataRow(6, 8)]
    [DataRow(7, 9)]
    [DataRow(8, 10)]
    public void Encode_WhenPartialFinalGroupForGit_ShouldEmitCompactLength(int byteCount, int expectedLength)
    {
        byte[] bytes = new byte[byteCount];
        for (int i = 0; i < byteCount; i++)
            bytes[i] = (byte)(i + 1);

        string actual = Base85.Encode(bytes, Base85Variant.Git);

        Assert.AreEqual(expectedLength, actual.Length);
    }

    /// <summary>
    /// Verifies that the byte-array and span overloads produce identical Git compact output.
    /// </summary>
    /// <param name="size">The input size.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(4)]
    [DataRow(8)]
    [DataRow(33)]
    public void Encode_ByteArrayAndSpanOverloadsForGit_ShouldProduceIdenticalOutput(int size)
    {
        byte[] bytes = new byte[size];
        for (int i = 0; i < size; i++)
            bytes[i] = (byte)((i * 11) ^ 0x9C);

        string fromArray = Base85.Encode(bytes, Base85Variant.Git);
        string fromSpan = Base85.Encode(bytes.AsSpan(), Base85Variant.Git);

        Assert.AreEqual(fromArray, fromSpan);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, Base85Variant, BaseFormattingOptions)" />
    /// returns <see langword="false" /> and writes zero when the destination is one character too small.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationTooSmallForGit_ShouldReturnFalseAndWriteZero()
    {
        byte[] bytes = Ascii("hello");
        int exact = Base85.GetEncodedLength(bytes, Base85Variant.Git);
        Span<char> destination = new char[exact - 1];

        bool success = Base85.TryEncode(bytes, destination, out int charsWritten, Base85Variant.Git);

        Assert.IsFalse(success);
        Assert.AreEqual(0, charsWritten);
    }
}
