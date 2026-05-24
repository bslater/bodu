// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Encoding.Preamble.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.HasPreamble(System.Text.Encoding)" /> returns
    /// <see langword="true" /> for BOM-enabled UTF-8 and <see langword="false" /> for plain UTF-8.
    /// </summary>
    /// <param name="emitBom">Whether to construct the UTF-8 encoding with a preamble.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DataRow(true, true)]
    [DataRow(false, false)]
    public void HasPreamble_ShouldReflectEncodingConfiguration(bool emitBom, bool expected)
    {
        System.Text.Encoding encoding = new System.Text.UTF8Encoding(emitBom);

        Assert.AreEqual(expected, encoding.HasPreamble());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.HasPreamble(System.Text.Encoding)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void HasPreamble_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.HasPreamble(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetPreambleLength(System.Text.Encoding)" /> returns the
    /// preamble length in bytes for each canonical encoding.
    /// </summary>
    /// <param name="codePage">The codepage of the encoding under test.</param>
    /// <param name="expected">The expected preamble length.</param>
    [DataTestMethod]
    [DataRow(65001, 3)]
    [DataRow(1200, 2)]
    [DataRow(1201, 2)]
    [DataRow(12000, 4)]
    [DataRow(20127, 0)]
    public void GetPreambleLength_ShouldMatchBclPreamble(int codePage, int expected)
    {
        var encoding = System.Text.Encoding.GetEncoding(codePage);

        Assert.AreEqual(expected, encoding.GetPreambleLength());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetPreambleLength(System.Text.Encoding)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetPreambleLength_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.GetPreambleLength(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryWritePreamble(System.Text.Encoding, Span{byte}, out int)" />
    /// writes the preamble when the destination fits and returns <see langword="false" /> when it does not.
    /// </summary>
    [TestMethod]
    public void TryWritePreamble_WhenDestinationFits_ShouldWritePreambleAndReportLength()
    {
        System.Text.Encoding utf8WithBom = new System.Text.UTF8Encoding(true);
        Span<byte> destination = new byte[16];

        var ok = utf8WithBom.TryWritePreamble(destination, out var written);

        Assert.IsTrue(ok);
        Assert.AreEqual(utf8WithBom.Preamble.Length, written);
        CollectionAssert.AreEqual(utf8WithBom.Preamble.ToArray(), destination.Slice(0, written).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryWritePreamble(System.Text.Encoding, Span{byte}, out int)" />
    /// returns <see langword="true" /> and zero bytes when the encoding has no preamble.
    /// </summary>
    [TestMethod]
    public void TryWritePreamble_WhenEncodingHasNoPreamble_ShouldReturnTrueWithZeroBytes()
    {
        Span<byte> destination = new byte[16];

        var ok = new System.Text.UTF8Encoding(false).TryWritePreamble(destination, out var written);

        Assert.IsTrue(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryWritePreamble(System.Text.Encoding, Span{byte}, out int)" />
    /// returns <see langword="false" /> when the destination is one byte too small.
    /// </summary>
    [TestMethod]
    public void TryWritePreamble_WhenDestinationIsTooSmall_ShouldReturnFalseAndZeroBytes()
    {
        System.Text.Encoding utf8WithBom = new System.Text.UTF8Encoding(true);
        var backing = new byte[utf8WithBom.Preamble.Length - 1];

        var ok = utf8WithBom.TryWritePreamble(backing, out var written);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.StartsWithPreamble(System.Text.Encoding, ReadOnlySpan{byte})" />
    /// returns <see langword="true" /> when the leading bytes match the preamble.
    /// </summary>
    [TestMethod]
    public void StartsWithPreamble_WhenBytesStartWithBom_ShouldReturnTrue()
    {
        System.Text.Encoding utf8WithBom = new System.Text.UTF8Encoding(true);
        var bytes = utf8WithBom.GetBytes(MultiByteText);
        var withPreamble = new byte[utf8WithBom.Preamble.Length + bytes.Length];
        utf8WithBom.Preamble.CopyTo(withPreamble);
        bytes.CopyTo(withPreamble, utf8WithBom.Preamble.Length);

        Assert.IsTrue(utf8WithBom.StartsWithPreamble(withPreamble));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.StartsWithPreamble(System.Text.Encoding, ReadOnlySpan{byte})" />
    /// returns <see langword="false" /> when the leading bytes do not match the preamble.
    /// </summary>
    [TestMethod]
    public void StartsWithPreamble_WhenBytesDoNotStartWithBom_ShouldReturnFalse()
    {
        System.Text.Encoding utf8WithBom = new System.Text.UTF8Encoding(true);

        Assert.IsFalse(utf8WithBom.StartsWithPreamble(System.Text.Encoding.UTF8.GetBytes(MultiByteText)));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.StartsWithPreamble(System.Text.Encoding, ReadOnlySpan{byte})" />
    /// returns <see langword="false" /> when the encoding has no preamble, even on an arbitrary byte sequence.
    /// </summary>
    [TestMethod]
    public void StartsWithPreamble_WhenEncodingHasNoPreamble_ShouldReturnFalse()
    {
        System.Text.Encoding utf8NoBom = new System.Text.UTF8Encoding(false);

        Assert.IsFalse(utf8NoBom.StartsWithPreamble(new byte[] { 0xEF, 0xBB, 0xBF }));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.StripPreamble(System.Text.Encoding, ReadOnlySpan{byte})" />
    /// removes the preamble when present.
    /// </summary>
    [TestMethod]
    public void StripPreamble_WhenBytesStartWithBom_ShouldReturnSpanAfterPreamble()
    {
        System.Text.Encoding utf8WithBom = new System.Text.UTF8Encoding(true);
        var payload = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var withPreamble = new byte[utf8WithBom.Preamble.Length + payload.Length];
        utf8WithBom.Preamble.CopyTo(withPreamble);
        payload.CopyTo(withPreamble, utf8WithBom.Preamble.Length);

        ReadOnlySpan<byte> result = utf8WithBom.StripPreamble(withPreamble);

        CollectionAssert.AreEqual(payload, result.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.StripPreamble(System.Text.Encoding, ReadOnlySpan{byte})" />
    /// returns the input unchanged when the preamble is not present.
    /// </summary>
    [TestMethod]
    public void StripPreamble_WhenBytesDoNotStartWithBom_ShouldReturnInputUnchanged()
    {
        System.Text.Encoding utf8WithBom = new System.Text.UTF8Encoding(true);
        var payload = System.Text.Encoding.UTF8.GetBytes(MultiByteText);

        ReadOnlySpan<byte> result = utf8WithBom.StripPreamble(payload);

        CollectionAssert.AreEqual(payload, result.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetStringSkippingPreamble(System.Text.Encoding, ReadOnlySpan{byte})" />
    /// decodes a BOM-prefixed UTF-8 byte sequence back to the original string.
    /// </summary>
    [TestMethod]
    public void GetStringSkippingPreamble_WhenBytesStartWithBom_ShouldDecodeWithoutBom()
    {
        System.Text.Encoding utf8WithBom = new System.Text.UTF8Encoding(true);
        var payload = utf8WithBom.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(MultiByteText))
            .ToArray();

        var actual = utf8WithBom.GetStringSkippingPreamble(payload);

        Assert.AreEqual(MultiByteText, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetByteCountWithPreamble(System.Text.Encoding, ReadOnlySpan{char})" />
    /// returns the encoded byte count plus the preamble length.
    /// </summary>
    [TestMethod]
    public void GetByteCountWithPreamble_WhenEncodingHasPreamble_ShouldIncludePreambleLength()
    {
        System.Text.Encoding utf8WithBom = new System.Text.UTF8Encoding(true);
        var expected = utf8WithBom.Preamble.Length + utf8WithBom.GetByteCount(MultiByteText);

        Assert.AreEqual(expected, utf8WithBom.GetByteCountWithPreamble(MultiByteText));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesWithPreamble(System.Text.Encoding, ReadOnlySpan{char})" />
    /// returns a byte array equal to the preamble concatenated with the encoded bytes.
    /// </summary>
    [TestMethod]
    public void GetBytesWithPreamble_WhenEncodingHasPreamble_ShouldPrefixPreamble()
    {
        System.Text.Encoding utf8WithBom = new System.Text.UTF8Encoding(true);

        var actual = utf8WithBom.GetBytesWithPreamble(MultiByteText);

        var expected = utf8WithBom.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(MultiByteText))
            .ToArray();
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesWithPreamble(System.Text.Encoding, ReadOnlySpan{char}, Span{byte})" />
    /// writes the preamble followed by the encoded bytes and returns the total count.
    /// </summary>
    [TestMethod]
    public void GetBytesWithPreamble_SpanOverload_WhenDestinationFits_ShouldWriteAndReturnTotal()
    {
        System.Text.Encoding utf8WithBom = new System.Text.UTF8Encoding(true);
        var total = utf8WithBom.GetByteCountWithPreamble(MultiByteText);
        Span<byte> destination = new byte[total];

        var written = utf8WithBom.GetBytesWithPreamble(MultiByteText, destination);

        Assert.AreEqual(total, written);
        var expected = utf8WithBom.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(MultiByteText))
            .ToArray();
        CollectionAssert.AreEqual(expected, destination.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesWithPreamble(System.Text.Encoding, ReadOnlySpan{char}, Span{byte})" />
    /// throws <see cref="ArgumentException" /> when the destination is too small.
    /// </summary>
    [TestMethod]
    public void GetBytesWithPreamble_SpanOverload_WhenDestinationIsTooSmall_ShouldThrowExactly()
    {
        System.Text.Encoding utf8WithBom = new System.Text.UTF8Encoding(true);
        var total = utf8WithBom.GetByteCountWithPreamble(MultiByteText);
        var backing = new byte[total - 1];

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = utf8WithBom.GetBytesWithPreamble(MultiByteText, backing);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryGetBytesWithPreamble(System.Text.Encoding, ReadOnlySpan{char}, Span{byte}, out int)" />
    /// returns <see langword="true" /> when the destination fits, and <see langword="false" /> when it does not.
    /// </summary>
    /// <param name="extra">Extra capacity added to the required buffer length (negative to undersize).</param>
    /// <param name="expectedOk">Whether the call is expected to succeed.</param>
    [DataTestMethod]
    [DataRow(0, true)]
    [DataRow(1, true)]
    [DataRow(-1, false)]
    public void TryGetBytesWithPreamble_ShouldRespectDestinationSize(int extra, bool expectedOk)
    {
        System.Text.Encoding utf8WithBom = new System.Text.UTF8Encoding(true);
        var total = utf8WithBom.GetByteCountWithPreamble(MultiByteText);
        var backing = new byte[total + extra];

        var ok = utf8WithBom.TryGetBytesWithPreamble(MultiByteText, backing, out var written);

        Assert.AreEqual(expectedOk, ok);
        Assert.AreEqual(expectedOk ? total : 0, written);
    }

    /// <summary>
    /// Verifies that the preamble-related extension methods all throw <see cref="ArgumentNullException" /> when
    /// <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void PreambleExtensions_WhenEncodingIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => EncodingExtensions.StartsWithPreamble(null!, new byte[] { 0x68 }));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.StripPreamble(null!, new byte[] { 0x68 }));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.GetStringSkippingPreamble(null!, new byte[] { 0x68 }));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.GetByteCountWithPreamble(null!, SampleText));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.GetBytesWithPreamble(null!, SampleText));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.GetBytesWithPreamble(null!, SampleText, new byte[64]));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.TryGetBytesWithPreamble(null!, SampleText, new byte[64], out _));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.TryWritePreamble(null!, new byte[64], out _));
    }
}
