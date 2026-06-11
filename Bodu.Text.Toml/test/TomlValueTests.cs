// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlValueTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Verifies the <see cref="TomlValue" /> scalar surface: the <c>Create</c> factory overloads for all eight TOML scalar
/// kinds, the typed conversions through <see cref="TomlValue.GetValue{T}" /> and
/// <see cref="TomlValue.TryGetValue{T}" /> including checked integer narrowing, and canonical rendering through
/// <see cref="TomlValue.ToString" /> and serialization.
/// </summary>
[TestClass]
public class TomlValueTests
{
    /// <summary>
    /// Verifies that <see cref="TomlValue.Create(string)" /> produces a string-kind value readable as
    /// <see cref="string" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenString_ShouldReportStringKind()
    {
        TomlValue value = TomlValue.Create("héllo");

        Assert.AreEqual(TomlValueKind.String, value.GetValueKind());
        Assert.AreEqual("héllo", value.GetValue<string>());
    }

    /// <summary>
    /// Verifies that <see cref="TomlValue.Create(string)" /> throws <see cref="ArgumentNullException" /> with
    /// <c>ParamName</c> <c>value</c> when the string is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenStringIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = TomlValue.Create((string)null!);
        }, "value");
    }

    /// <summary>
    /// Verifies that <see cref="TomlValue.Create(long)" /> produces an integer-kind value readable as
    /// <see cref="long" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenInt64_ShouldReportIntegerKind()
    {
        TomlValue value = TomlValue.Create(long.MaxValue);

        Assert.AreEqual(TomlValueKind.Integer, value.GetValueKind());
        Assert.AreEqual(long.MaxValue, value.GetValue<long>());
    }

    /// <summary>
    /// Verifies that <see cref="TomlValue.Create(int)" /> stores the value as a 64-bit integer.
    /// </summary>
    [TestMethod]
    public void Create_WhenInt32_ShouldStoreAsInteger()
    {
        TomlValue value = TomlValue.Create(42);

        Assert.AreEqual(TomlValueKind.Integer, value.GetValueKind());
        Assert.AreEqual(42L, value.GetValue<long>());
    }

    /// <summary>
    /// Verifies that <see cref="TomlValue.Create(double)" /> produces a float-kind value readable as
    /// <see cref="double" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenDouble_ShouldReportFloatKind()
    {
        TomlValue value = TomlValue.Create(1.5d);

        Assert.AreEqual(TomlValueKind.Float, value.GetValueKind());
        Assert.AreEqual(1.5d, value.GetValue<double>());
    }

    /// <summary>
    /// Verifies that <see cref="TomlValue.Create(bool)" /> produces a Boolean-kind value readable as
    /// <see cref="bool" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenBoolean_ShouldReportBooleanKind()
    {
        TomlValue value = TomlValue.Create(true);

        Assert.AreEqual(TomlValueKind.Boolean, value.GetValueKind());
        Assert.IsTrue(value.GetValue<bool>());
    }

    /// <summary>
    /// Verifies that each date-time factory produces a value of the matching kind readable as the matching CLR type.
    /// </summary>
    [TestMethod]
    public void Create_WhenDateTimeKinds_ShouldReportMatchingKindsAndValues()
    {
        var odt = new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.FromHours(2));
        var ldt = new DateTime(1979, 5, 27, 7, 32, 0);
        var ld = new DateOnly(1979, 5, 27);
        var lt = new TimeOnly(7, 32, 0);

        Assert.AreEqual(TomlValueKind.OffsetDateTime, TomlValue.Create(odt).GetValueKind());
        Assert.AreEqual(odt, TomlValue.Create(odt).GetValue<DateTimeOffset>());
        Assert.AreEqual(TomlValueKind.LocalDateTime, TomlValue.Create(ldt).GetValueKind());
        Assert.AreEqual(ldt, TomlValue.Create(ldt).GetValue<DateTime>());
        Assert.AreEqual(TomlValueKind.LocalDate, TomlValue.Create(ld).GetValueKind());
        Assert.AreEqual(ld, TomlValue.Create(ld).GetValue<DateOnly>());
        Assert.AreEqual(TomlValueKind.LocalTime, TomlValue.Create(lt).GetValueKind());
        Assert.AreEqual(lt, TomlValue.Create(lt).GetValue<TimeOnly>());
    }

    /// <summary>
    /// Verifies that an in-range integer converts to every fixed-width integer type through
    /// <see cref="TomlValue.GetValue{T}" />.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenIntegerInRangeOfEveryWidth_ShouldConvert()
    {
        TomlValue value = TomlValue.Create(42L);

        Assert.AreEqual((sbyte)42, value.GetValue<sbyte>());
        Assert.AreEqual((byte)42, value.GetValue<byte>());
        Assert.AreEqual((short)42, value.GetValue<short>());
        Assert.AreEqual((ushort)42, value.GetValue<ushort>());
        Assert.AreEqual(42, value.GetValue<int>());
        Assert.AreEqual(42U, value.GetValue<uint>());
        Assert.AreEqual(42L, value.GetValue<long>());
        Assert.AreEqual(42UL, value.GetValue<ulong>());
    }

    /// <summary>
    /// Verifies that reading an integer as a narrower type whose range it exceeds throws
    /// <see cref="InvalidOperationException" />, because the checked conversion is out of range.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenIntegerOutOfRangeOfNarrowerType_ShouldThrowInvalidOperationException()
    {
        TomlValue value = TomlValue.Create(300L);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = value.GetValue<byte>();
        });
    }

    /// <summary>
    /// Verifies that a negative integer read as <see cref="ulong" /> throws
    /// <see cref="InvalidOperationException" />, because the checked conversion is out of range.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenNegativeIntegerReadAsUInt64_ShouldThrowInvalidOperationException()
    {
        TomlValue value = TomlValue.Create(-1L);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = value.GetValue<ulong>();
        });
    }

    /// <summary>
    /// Verifies that a float value reads back as both <see cref="double" /> and <see cref="float" />, the two
    /// floating-point conversions the model supports.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenFloatReadAsDoubleOrSingle_ShouldConvert()
    {
        TomlValue value = TomlValue.Create(1.5d);

        Assert.AreEqual(1.5d, value.GetValue<double>());
        Assert.AreEqual(1.5f, value.GetValue<float>());
    }

    /// <summary>
    /// Verifies that <see cref="TomlValue.TryGetValue{T}" /> returns <see langword="false" /> for cross-kind
    /// combinations: an integer is not a float or Boolean, and a float is not an integer.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKindAndTypeMismatch_ShouldReturnFalse()
    {
        Assert.IsFalse(TomlValue.Create(1L).TryGetValue(out double asDouble));
        Assert.AreEqual(0d, asDouble);
        Assert.IsFalse(TomlValue.Create(1L).TryGetValue(out bool asBoolean));
        Assert.IsFalse(asBoolean);
        Assert.IsFalse(TomlValue.Create(1.0d).TryGetValue(out long asInt64));
        Assert.AreEqual(0L, asInt64);
        Assert.IsFalse(TomlValue.Create("x").TryGetValue(out long stringAsInt64));
        Assert.AreEqual(0L, stringAsInt64);
    }

    /// <summary>
    /// Verifies that <see cref="TomlValue.TryGetValue{T}" /> returns <see langword="false" /> for date-time kinds
    /// requested as a different date-time CLR type.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenDateTimeKindsCrossRequested_ShouldReturnFalse()
    {
        Assert.IsFalse(TomlValue.Create(new DateTime(1979, 5, 27)).TryGetValue(out DateTimeOffset _));
        Assert.IsFalse(TomlValue.Create(new DateTimeOffset(1979, 5, 27, 0, 0, 0, TimeSpan.Zero)).TryGetValue(out DateTime _));
        Assert.IsFalse(TomlValue.Create(new DateOnly(1979, 5, 27)).TryGetValue(out TimeOnly _));
    }

    /// <summary>
    /// Verifies that <see cref="TomlValue.GetValue{T}" /> throws <see cref="InvalidOperationException" /> when the
    /// stored kind cannot convert to the requested type.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenStringReadAsInt64_ShouldThrowInvalidOperationException()
    {
        TomlValue value = TomlValue.Create("hello");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = value.GetValue<long>();
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlValue.ToString" /> renders each scalar kind in its invariant text form.
    /// </summary>
    [TestMethod]
    public void ToString_WhenScalarKindVaries_ShouldRenderInvariantText()
    {
        Assert.AreEqual("héllo", TomlValue.Create("héllo").ToString());
        Assert.AreEqual("-42", TomlValue.Create(-42L).ToString());
        Assert.AreEqual("1.5", TomlValue.Create(1.5d).ToString());
        Assert.AreEqual("true", TomlValue.Create(true).ToString());
        Assert.AreEqual("false", TomlValue.Create(false).ToString());
        Assert.AreEqual(
            "1979-05-27T07:32:00.0000000+02:00",
            TomlValue.Create(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.FromHours(2))).ToString());
        Assert.AreEqual("1979-05-27T07:32:00", TomlValue.Create(new DateTime(1979, 5, 27, 7, 32, 0)).ToString());
        Assert.AreEqual("1979-05-27", TomlValue.Create(new DateOnly(1979, 5, 27)).ToString());
        Assert.AreEqual("07:32:00.0000000", TomlValue.Create(new TimeOnly(7, 32, 0)).ToString());
    }

    /// <summary>
    /// Verifies that the special float values render through <see cref="TomlValue.ToString" /> as the invariant
    /// <see cref="double" /> spellings rather than the TOML literals.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSpecialFloat_ShouldRenderInvariantDoubleSpelling()
    {
        Assert.AreEqual("Infinity", TomlValue.Create(double.PositiveInfinity).ToString());
        Assert.AreEqual("-Infinity", TomlValue.Create(double.NegativeInfinity).ToString());
        Assert.AreEqual("NaN", TomlValue.Create(double.NaN).ToString());
    }

    /// <summary>
    /// Verifies that each scalar kind serializes to its canonical TOML value text when written as a table member.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenScalarKindVaries_ShouldEmitCanonicalTomlValue()
    {
        var root = new TomlObject();
        root["s"] = TomlValue.Create("x");
        root["i"] = TomlValue.Create(-7L);
        root["f"] = TomlValue.Create(1.5d);
        root["b"] = TomlValue.Create(false);
        root["odt"] = TomlValue.Create(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero));
        root["ldt"] = TomlValue.Create(new DateTime(1979, 5, 27, 7, 32, 0));
        root["ld"] = TomlValue.Create(new DateOnly(1979, 5, 27));
        root["lt"] = TomlValue.Create(new TimeOnly(7, 32, 0));

        string expected =
            "s = \"x\"\n" +
            "i = -7\n" +
            "f = 1.5\n" +
            "b = false\n" +
            "odt = 1979-05-27T07:32:00Z\n" +
            "ldt = 1979-05-27T07:32:00\n" +
            "ld = 1979-05-27\n" +
            "lt = 07:32:00\n";

        Assert.AreEqual(expected, root.ToString());
    }

    /// <summary>
    /// Verifies that the special float values serialize to the TOML literals <c>inf</c>, <c>-inf</c>, and
    /// <c>nan</c>.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenSpecialFloat_ShouldEmitTomlLiterals()
    {
        var root = new TomlObject();
        root["pi"] = TomlValue.Create(double.PositiveInfinity);
        root["ni"] = TomlValue.Create(double.NegativeInfinity);
        root["n"] = TomlValue.Create(double.NaN);

        Assert.AreEqual("pi = inf\nni = -inf\nn = nan\n", root.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="TomlValue.DeepClone" /> copies the kind and payload into an independent, parentless
    /// node that compares deeply equal to the original.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenScalarCloned_ShouldCopyKindAndPayload()
    {
        var owner = new TomlArray();
        TomlValue original = TomlValue.Create(new DateOnly(1979, 5, 27));
        owner.Add(original);

        TomlValue clone = original.DeepClone().AsValue();

        Assert.IsNull(clone.Parent);
        Assert.AreEqual(TomlValueKind.LocalDate, clone.GetValueKind());
        Assert.IsTrue(TomlNode.DeepEquals(original, clone));
    }
}
