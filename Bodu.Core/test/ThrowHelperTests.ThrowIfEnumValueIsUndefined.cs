// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfEnumValueIsUndefined.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEnumValueIsUndefined{TEnum}" /> does not throw - and on
    /// the ParamName-asserting overload reports nothing - for defined enum values.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The enum value passed to the guard.</param>
    [TestMethod]
    [DataRow("defined A", TestEnum.A)]
    [DataRow("defined B", TestEnum.B)]
    public void ThrowIfEnumValueIsUndefined_WhenValueIsDefined_ShouldNotThrowAndReportNothing(string testName, TestEnum value) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfEnumValueIsUndefined(value, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEnumValueIsUndefined{TEnum}" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for undefined enum values.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The enum value passed to the guard.</param>
    [TestMethod]
    [DataRow("undefined 99", (TestEnum)99)]
    [DataRow("undefined -1", (TestEnum)(-1))]
    public void ThrowIfEnumValueIsUndefined_WhenValueIsUndefined_ShouldThrowOnValue(string testName, TestEnum value) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfEnumValueIsUndefined(value, nameof(value)),
            typeof(ArgumentOutOfRangeException),
            "value");

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEnumValueIsUndefined" />, when ValueIsDefined, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(TestEnum.A)]
    [DataRow(TestEnum.B)]
    public void ThrowIfEnumValueIsUndefined_WhenValueIsDefined_ShouldNotThrow(TestEnum value) => ThrowHelper.ThrowIfEnumValueIsUndefined(value);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEnumValueIsUndefined" />, when ValueIsUndefined, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow((TestEnum)99)]
    [DataRow((TestEnum)(-1))]
    public void ThrowIfEnumValueIsUndefined_WhenValueIsUndefined_ShouldThrowExactly(TestEnum value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);
        });
    }

    /// <summary>
    /// Verifies that every defined value of enumerations whose values run contiguously passes, for signed and unsigned
    /// underlying types of several widths, including both ends of each range.
    /// </summary>
    [TestMethod]
    public void ThrowIfEnumValueIsUndefined_WhenContiguousEnumValueIsDefined_ShouldNotThrow()
    {
        AssertEveryDefinedValueAccepted<DayOfWeek>();
        AssertEveryDefinedValueAccepted<Bodu.Extensions.WeekOrdinal>();
        AssertEveryDefinedValueAccepted<SignedByteEnum>();
        AssertEveryDefinedValueAccepted<ByteEnum>();
        AssertEveryDefinedValueAccepted<UnsignedLongEnum>();
    }

    /// <summary>
    /// Verifies that a value just outside, or far outside, the range of an enumeration whose values run contiguously
    /// throws <see cref="ArgumentOutOfRangeException" /> naming the argument, with the resource message.
    /// </summary>
    [TestMethod]
    public void ThrowIfEnumValueIsUndefined_WhenContiguousEnumValueIsOutsideItsRange_ShouldThrowNamingTheArgument()
    {
        AssertEnumValueRejected((DayOfWeek)(-1));
        AssertEnumValueRejected((DayOfWeek)7);
        AssertEnumValueRejected((DayOfWeek)int.MinValue);
        AssertEnumValueRejected((DayOfWeek)int.MaxValue);
        AssertEnumValueRejected((Bodu.Extensions.WeekOrdinal)0);
        AssertEnumValueRejected((Bodu.Extensions.WeekOrdinal)7);
        AssertEnumValueRejected((SignedByteEnum)(-3));
        AssertEnumValueRejected((SignedByteEnum)2);
        AssertEnumValueRejected((SignedByteEnum)sbyte.MinValue);
        AssertEnumValueRejected((SignedByteEnum)sbyte.MaxValue);
        AssertEnumValueRejected((ByteEnum)0);
        AssertEnumValueRejected((ByteEnum)252);
        AssertEnumValueRejected((UnsignedLongEnum)0);
        AssertEnumValueRejected((UnsignedLongEnum)(ulong.MaxValue - 2));
        AssertEnumValueRejected((UnsignedLongEnum)long.MaxValue);
    }

    /// <summary>
    /// Verifies that every defined value of an enumeration with gaps between its values passes.
    /// </summary>
    [TestMethod]
    public void ThrowIfEnumValueIsUndefined_WhenGappedEnumValueIsDefined_ShouldNotThrow() =>
        AssertEveryDefinedValueAccepted<GappedEnum>();

    /// <summary>
    /// Verifies that a value in a gap between the defined values of an enumeration, or beyond either end of them,
    /// throws <see cref="ArgumentOutOfRangeException" /> naming the argument, with the resource message.
    /// </summary>
    [TestMethod]
    public void ThrowIfEnumValueIsUndefined_WhenGappedEnumValueIsInAGapOrOutside_ShouldThrowNamingTheArgument()
    {
        AssertEnumValueRejected((GappedEnum)(-3));
        AssertEnumValueRejected((GappedEnum)(-1));
        AssertEnumValueRejected((GappedEnum)1);
        AssertEnumValueRejected((GappedEnum)4);
        AssertEnumValueRejected((GappedEnum)6);
    }

    /// <summary>
    /// Verifies that every named value of a <see cref="FlagsAttribute" /> enumeration passes.
    /// </summary>
    [TestMethod]
    public void ThrowIfEnumValueIsUndefined_WhenFlagsEnumValueIsNamed_ShouldNotThrow() =>
        AssertEveryDefinedValueAccepted<FlagsEnum>();

    /// <summary>
    /// Verifies that a combination of a <see cref="FlagsAttribute" /> enumeration's flags that no name denotes, or a
    /// value outside them, throws <see cref="ArgumentOutOfRangeException" /> naming the argument, as
    /// <see cref="Enum.IsDefined{TEnum}(TEnum)" /> decides.
    /// </summary>
    [TestMethod]
    public void ThrowIfEnumValueIsUndefined_WhenFlagsEnumValueIsAnUnnamedCombinationOrOutside_ShouldThrowNamingTheArgument()
    {
        AssertEnumValueRejected((FlagsEnum)3);
        AssertEnumValueRejected((FlagsEnum)7);
        AssertEnumValueRejected((FlagsEnum)8);
        AssertEnumValueRejected((FlagsEnum)(-1));
    }

    /// <summary>
    /// Asserts that every value an enumeration defines passes <see cref="ThrowHelper.ThrowIfEnumValueIsUndefined{TEnum}" />.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    private static void AssertEveryDefinedValueAccepted<TEnum>()
        where TEnum : struct, Enum
    {
        foreach (TEnum value in Enum.GetValues<TEnum>())
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);
    }

    /// <summary>
    /// Asserts that <see cref="ThrowHelper.ThrowIfEnumValueIsUndefined{TEnum}" /> rejects a value with
    /// <see cref="ArgumentOutOfRangeException" />, naming the argument and carrying the resource message.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The undefined value.</param>
    private static void AssertEnumValueRejected<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);
        });

        string expected = new ArgumentOutOfRangeException(
            nameof(value),
            string.Format(System.Globalization.CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_EnumValue, typeof(TEnum).Name, value)).Message;
        Assert.AreEqual("value", ex.ParamName, $"{typeof(TEnum).Name} {value}");
        Assert.AreEqual(expected, ex.Message, $"{typeof(TEnum).Name} {value}");
    }
}
