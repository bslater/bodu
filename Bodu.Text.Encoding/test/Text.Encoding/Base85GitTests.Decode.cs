// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85GitTests.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Encoding;

public sealed partial class Base85GitTests
{
    /// <summary>
    /// Verifies that <see cref="Base85.Decode(ReadOnlySpan{char}, Base85Variant, BaseFormatStyles)" /> recovers the
    /// original bytes for every Git compact-mode Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(Base85GitKnownAnswerVectors.GitCompactVectors), typeof(Base85GitKnownAnswerVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decode_ForGitCompactKnownAnswerVector_ShouldRecoverBytes(EncodingKnownAnswerVector vector)
    {
        byte[] actual = Base85.Decode(vector.Encoded, Base85Variant.GitCompact);

        CollectionAssert.AreEqual(vector.DecodedBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(ReadOnlySpan{char}, Base85Variant, BaseFormatStyles)" /> throws the
    /// expected exception type for every malformed Git compact-mode input.
    /// </summary>
    /// <param name="vector">A negative KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(Base85GitKnownAnswerVectors.GitNegativeVectors), typeof(Base85GitKnownAnswerVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decode_ForGitMalformedInput_ShouldThrowExactly(EncodingNegativeDecodeVector vector)
    {
        Exception? actual = null;
        try
        {
            _ = Base85.Decode(vector.MalformedInput, Base85Variant.GitCompact);
        }
        catch (Exception ex)
        {
            actual = ex;
        }

        Assert.IsNotNull(actual, $"No exception thrown for: {vector}");
        Assert.AreEqual(vector.ExpectedExceptionType, actual.GetType(), $"Wrong exception type for: {vector}; actual was {actual.GetType().Name}.");
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, Base85Variant, BaseFormatStyles)" />
    /// returns <see langword="false" /> for every malformed Git compact-mode input.
    /// </summary>
    /// <param name="vector">A negative KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(Base85GitKnownAnswerVectors.GitNegativeVectors), typeof(Base85GitKnownAnswerVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryDecode_ForGitMalformedInput_ShouldReturnFalse(EncodingNegativeDecodeVector vector)
    {
        Span<byte> destination = new byte[vector.MalformedInput.Length + 32];

        bool success = Base85.TryDecode(vector.MalformedInput.AsSpan(), destination, out _, Base85Variant.GitCompact);

        Assert.IsFalse(success);
    }

    /// <summary>
    /// Verifies that Git decode ignores whitespace only when
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> is supplied.
    /// </summary>
    [TestMethod]
    public void Decode_WhenWhitespaceAndIgnoreWhitespaceForGit_ShouldRecoverBytes()
    {
        byte[] decoded = Base85.Decode("0Rj UA", Base85Variant.GitCompact, BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 }, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, Base85Variant, BaseFormatStyles)" />
    /// returns <see langword="false" /> and writes zero when the destination is one byte too small.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenDestinationTooSmallForGit_ShouldReturnFalseAndWriteZero()
    {
        Span<byte> destination = new byte[4];

        bool success = Base85.TryDecode("Xk~0{Zv".AsSpan(), destination, out int bytesWritten, Base85Variant.GitCompact);

        Assert.IsFalse(success);
        Assert.AreEqual(0, bytesWritten);
    }
}
