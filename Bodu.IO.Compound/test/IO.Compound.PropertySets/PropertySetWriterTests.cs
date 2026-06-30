// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Verifies that <see cref="OlePropertySet.ToArray" /> writes property values that read back identically through
/// <see cref="OlePropertySet.Parse(ReadOnlyMemory{byte})" />.
/// </summary>
[TestClass]
public class PropertySetWriterTests
{
    /// <summary>The format identifier used for the test sections.</summary>
    private static readonly Guid TestFormatId = new("F29F85E0-4FF9-1068-AB91-08002B27B3D9");

    /// <summary>
    /// Round-trips a single property of a given type through write and parse and returns the parsed value.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <returns>The value read back.</returns>
    private static OlePropertyValue RoundTrip(OlePropertyValue value)
    {
        var section = new OlePropertySection(TestFormatId, 1252);
        section.Set(10, value);
        var set = new OlePropertySet(TestFormatId, Guid.Empty, 1252);
        set.AddSection(section);

        var parsed = OlePropertySet.Parse(set.ToArray());
        Assert.IsNotNull(parsed[10]);
        return parsed[10]!;
    }

    /// <summary>
    /// Verifies that an ANSI string round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenAnsiString_ShouldRoundTrip() =>
        Assert.AreEqual("hello world", RoundTrip(OlePropertyValue.Create("hello world")).AsString());

    /// <summary>
    /// Verifies that an odd-length ANSI string round-trips, exercising four-byte alignment padding.
    /// </summary>
    [TestMethod]
    public void Write_WhenOddLengthString_ShouldRoundTrip() =>
        Assert.AreEqual("abc", RoundTrip(OlePropertyValue.Create("abc")).AsString());

    /// <summary>
    /// Verifies that a UTF-16 string round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenUnicodeString_ShouldRoundTrip() =>
        Assert.AreEqual("café", RoundTrip(OlePropertyValue.Create("café", OlePropertyType.UnicodeString)).AsString());

    /// <summary>
    /// Verifies that a 32-bit integer round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenInt32_ShouldRoundTrip() =>
        Assert.AreEqual(123456, RoundTrip(OlePropertyValue.Create(123456)).AsInt32());

    /// <summary>
    /// Verifies that a 16-bit integer round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenInt16_ShouldRoundTrip() =>
        Assert.AreEqual(-12345, RoundTrip(OlePropertyValue.Create((short)-12345)).AsInt32());

    /// <summary>
    /// Verifies that a 64-bit integer round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenInt64_ShouldRoundTrip() =>
        Assert.AreEqual(9_000_000_000L, RoundTrip(OlePropertyValue.Create(9_000_000_000L)).AsInt64());

    /// <summary>
    /// Verifies that a double round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenDouble_ShouldRoundTrip() =>
        Assert.AreEqual(3.14159, RoundTrip(OlePropertyValue.Create(3.14159)).GetValueOrDefault<double>());

    /// <summary>
    /// Verifies that a boolean round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenBoolean_ShouldRoundTrip() =>
        Assert.IsTrue(RoundTrip(OlePropertyValue.Create(true)).AsBoolean());

    /// <summary>
    /// Verifies that a FILETIME round-trips as a point in time.
    /// </summary>
    [TestMethod]
    public void Write_WhenFileTime_ShouldRoundTrip()
    {
        var time = new DateTimeOffset(2021, 5, 6, 7, 8, 9, TimeSpan.Zero);

        Assert.AreEqual(time, RoundTrip(OlePropertyValue.Create(time)).AsDateTimeOffset());
    }

    /// <summary>
    /// Verifies that a blob round-trips, including an odd length that requires alignment padding.
    /// </summary>
    [TestMethod]
    public void Write_WhenBlob_ShouldRoundTrip()
    {
        byte[] payload = [1, 2, 3, 4, 5];

        CollectionAssert.AreEqual(payload, RoundTrip(OlePropertyValue.CreateBlob(payload)).AsBytes());
    }

    /// <summary>
    /// Verifies that the written section reports the configured code page.
    /// </summary>
    [TestMethod]
    public void Write_WhenSection_ShouldPreserveCodePage()
    {
        var section = new OlePropertySection(TestFormatId, 1252);
        section.Set(2, OlePropertyValue.Create("x"));
        var set = new OlePropertySet(TestFormatId, Guid.Empty, 1252);
        set.AddSection(section);

        var parsed = OlePropertySet.Parse(set.ToArray());

        Assert.AreEqual(1252, parsed.Sections[0].CodePage);
        Assert.AreEqual(TestFormatId, parsed.FormatId);
    }
}
