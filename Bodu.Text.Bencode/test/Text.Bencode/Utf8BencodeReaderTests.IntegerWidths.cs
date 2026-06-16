// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderTests.IntegerWidths.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the width-specific integer accessors <see cref="Utf8BencodeReader.GetInt32" />,
/// <see cref="Utf8BencodeReader.TryGetInt32" />, and <see cref="Utf8BencodeReader.TryGetInt64" /> that mirror the
/// <see cref="System.Text.Json.Utf8JsonReader" /> integer family.
/// </summary>
public partial class Utf8BencodeReaderTests
{
    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.GetInt32" /> reads values across the full <see cref="int" />
    /// range, including both extremes.
    /// </summary>
    /// <param name="input">The Bencode integer token.</param>
    /// <param name="expected">The expected value.</param>
    [TestMethod]
    [DataRow("i0e", 0)]
    [DataRow("i-1e", -1)]
    [DataRow("i2147483647e", int.MaxValue)]
    [DataRow("i-2147483648e", int.MinValue)]
    public void GetInt32_WhenValueFitsInt32_ShouldReturnValue(string input, int expected)
    {
        var reader = new Utf8BencodeReader(Bytes(input));
        Assert.IsTrue(reader.Read());

        Assert.AreEqual(expected, reader.GetInt32());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.GetInt32" /> throws <see cref="BencodeFormatException" /> when the
    /// integer token's value is outside the <see cref="int" /> range in either direction.
    /// </summary>
    /// <param name="input">The Bencode integer token.</param>
    [TestMethod]
    [DataRow("i2147483648e")]
    [DataRow("i-2147483649e")]
    [DataRow("i9223372036854775808e")]
    public void GetInt32_WhenValueOutsideInt32Range_ShouldThrowBencodeFormatException(string input)
    {
        byte[] data = Bytes(input);

        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            var reader = new Utf8BencodeReader(data);
            _ = reader.Read();
            _ = reader.GetInt32();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.TryGetInt32" /> reports range fit without throwing, returning the
    /// value only when it is representable.
    /// </summary>
    [TestMethod]
    public void TryGetInt32_WhenValueOutsideInt32Range_ShouldReturnFalse()
    {
        var fits = new Utf8BencodeReader(Bytes("i42e"));
        Assert.IsTrue(fits.Read());
        Assert.IsTrue(fits.TryGetInt32(out int value));
        Assert.AreEqual(42, value);

        var exceeds = new Utf8BencodeReader(Bytes("i2147483648e"));
        Assert.IsTrue(exceeds.Read());
        Assert.IsFalse(exceeds.TryGetInt32(out int overflow));
        Assert.AreEqual(0, overflow);
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.TryGetInt64" /> returns the value for the signed 64-bit range and
    /// reports <see langword="false" /> for values readable only through <see cref="Utf8BencodeReader.GetUInt64" />.
    /// </summary>
    [TestMethod]
    public void TryGetInt64_WhenValueExceedsInt64_ShouldReturnFalse()
    {
        var fits = new Utf8BencodeReader(Bytes("i-9223372036854775808e"));
        Assert.IsTrue(fits.Read());
        Assert.IsTrue(fits.TryGetInt64(out long value));
        Assert.AreEqual(long.MinValue, value);

        var exceeds = new Utf8BencodeReader(Bytes("i9223372036854775808e"));
        Assert.IsTrue(exceeds.Read());
        Assert.IsFalse(exceeds.TryGetInt64(out long overflow));
        Assert.AreEqual(0L, overflow);
        Assert.AreEqual(9223372036854775808UL, exceeds.GetUInt64());
    }

    /// <summary>
    /// Verifies that the width-specific accessors throw <see cref="InvalidOperationException" /> when the current
    /// token is not an integer.
    /// </summary>
    [TestMethod]
    public void GetInt32_WhenTokenIsNotInteger_ShouldThrowInvalidOperationException()
    {
        byte[] data = Bytes("4:spam");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(data);
            _ = reader.Read();
            _ = reader.GetInt32();
        });

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(data);
            _ = reader.Read();
            _ = reader.TryGetInt32(out _);
        });

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(data);
            _ = reader.Read();
            _ = reader.TryGetInt64(out _);
        });
    }
}
