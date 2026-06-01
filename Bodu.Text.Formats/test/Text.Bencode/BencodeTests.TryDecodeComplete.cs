// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.TryDecodeComplete.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public sealed partial class BencodeTests
{
    /// <summary>
    /// Verifies that <see cref="Bencode.TryDecode(ReadOnlySpan{byte}, out BencodedValue, out int)" /> in its
    /// lenient (default) shape returns <see langword="true" /> when the prefix of the source decodes successfully
    /// even though trailing bytes follow.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenTrailingBytesAndDefault_ShouldReturnTrueWithPrefixBytesConsumed()
    {
        var payload = Bytes("i1eXYZ");

        var success = Bencode.TryDecode(payload, out BencodedValue? value, out var bytesConsumed);

        Assert.IsTrue(success);
        Assert.IsNotNull(value);
        Assert.AreEqual(3, bytesConsumed);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.TryDecode(ReadOnlySpan{byte}, BencodeParseOptions, out BencodedValue, out int)" />
    /// with <see cref="BencodeParseOptions.RequireCompleteDocument" /> set to <see langword="true" /> rejects an
    /// input whose prefix decodes but is followed by trailing bytes.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenTrailingBytesAndRequireComplete_ShouldReturnFalse()
    {
        BencodeParseOptions options = new() { RequireCompleteDocument = true };
        var payload = Bytes("i1eXYZ");

        var success = Bencode.TryDecode(payload, options, out BencodedValue? value, out var bytesConsumed);

        Assert.IsFalse(success);
        Assert.IsNull(value);
        Assert.AreEqual(0, bytesConsumed);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.TryDecode(ReadOnlySpan{byte}, BencodeParseOptions, out BencodedValue, out int)" />
    /// with <see cref="BencodeParseOptions.RequireCompleteDocument" /> succeeds and reports the full source length
    /// when the entire input is consumed.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenInputFullyConsumedAndRequireComplete_ShouldReturnTrue()
    {
        BencodeParseOptions options = new() { RequireCompleteDocument = true };
        var payload = Bytes("i1e");

        var success = Bencode.TryDecode(payload, options, out BencodedValue? value, out var bytesConsumed);

        Assert.IsTrue(success);
        Assert.IsNotNull(value);
        Assert.AreEqual(payload.Length, bytesConsumed);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeParseOptions.RequireCompleteDocument" /> defaults to <see langword="false" />
    /// so the documented lenient TryDecode contract is preserved.
    /// </summary>
    [TestMethod]
    public void RequireCompleteDocument_WhenOptionsAreDefault_ShouldBeFalse()
    {
        BencodeParseOptions options = BencodeParseOptions.Default;

        Assert.IsFalse(options.RequireCompleteDocument);
    }
}
