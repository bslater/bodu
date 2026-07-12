// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Text.Serialization;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the parameter-validation contracts of <see cref="TomlSerializerOptions" />: undefined enumeration values
/// are rejected with the expected <c>ParamName</c> and converter resolution rejects a <see langword="null" /> type. The
/// converter list's null-rejection and read-only contract is covered in the <c>Converters</c> partial.
/// </summary>
[TestClass]
public partial class TomlSerializerOptionsTests
{
    /// <summary>
    /// Verifies that setting <see cref="TomlSerializerOptions.DefaultIgnoreCondition" /> to an undefined
    /// <see cref="IgnoreCondition" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>value</c>.
    /// </summary>
    [TestMethod]
    public void DefaultIgnoreCondition_WhenSetToUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new TomlSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            options.DefaultIgnoreCondition = (IgnoreCondition)99;
        }, "value");
    }

    /// <summary>
    /// Verifies that setting <see cref="TomlSerializerOptions.UnmappedMemberHandling" /> to an undefined
    /// <see cref="UnmappedMemberHandling" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>value</c>.
    /// </summary>
    [TestMethod]
    public void UnmappedMemberHandling_WhenSetToUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new TomlSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            options.UnmappedMemberHandling = (UnmappedMemberHandling)99;
        }, "value");
    }

    /// <summary>
    /// Verifies that setting <see cref="TomlSerializerOptions.PreferredObjectCreationHandling" /> to an undefined
    /// <see cref="ObjectCreationHandling" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>value</c>.
    /// </summary>
    [TestMethod]
    public void PreferredObjectCreationHandling_WhenSetToUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new TomlSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            options.PreferredObjectCreationHandling = (ObjectCreationHandling)99;
        }, "value");
    }

    /// <summary>
    /// Verifies that setting <see cref="TomlSerializerOptions.SpecVersion" /> to an undefined
    /// <see cref="TomlSpecVersion" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>value</c>.
    /// </summary>
    [TestMethod]
    public void SpecVersion_WhenSetToUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new TomlSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            options.SpecVersion = (TomlSpecVersion)99;
        }, "value");
    }

    /// <summary>
    /// Verifies that setting <see cref="TomlSerializerOptions.ByteArrayHandling" /> to an undefined
    /// <see cref="TomlByteArrayHandling" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>value</c>.
    /// </summary>
    [TestMethod]
    public void ByteArrayHandling_WhenSetToUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new TomlSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            options.ByteArrayHandling = (TomlByteArrayHandling)99;
        }, "value");
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializerOptions.DecimalHandling" /> defaults to
    /// <see cref="TomlDecimalHandling.Float" /> and round-trips an assigned value while the options are mutable.
    /// </summary>
    [TestMethod]
    public void DecimalHandling_WhenUnset_ShouldDefaultToFloatAndRoundTripAssignment()
    {
        var options = new TomlSerializerOptions();

        Assert.AreEqual(TomlDecimalHandling.Float, options.DecimalHandling);

        options.DecimalHandling = TomlDecimalHandling.String;

        Assert.AreEqual(TomlDecimalHandling.String, options.DecimalHandling);
    }

    /// <summary>
    /// Verifies that setting <see cref="TomlSerializerOptions.DecimalHandling" /> to an undefined
    /// <see cref="TomlDecimalHandling" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>value</c>.
    /// </summary>
    [TestMethod]
    public void DecimalHandling_WhenSetToUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new TomlSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            options.DecimalHandling = (TomlDecimalHandling)99;
        }, "value");
    }

    /// <summary>
    /// Verifies that setting <see cref="TomlSerializerOptions.DecimalHandling" /> after the options have become
    /// read-only throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void DecimalHandling_WhenSetAfterOptionsReadOnly_ShouldThrowInvalidOperationException()
    {
        var options = new TomlSerializerOptions();
        options.MakeReadOnly();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            options.DecimalHandling = TomlDecimalHandling.String;
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializerOptions.GetConverter(Type)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>typeToConvert</c> when the type is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetConverter_WhenTypeToConvertNull_ShouldThrowArgumentNullException()
    {
        var options = new TomlSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = options.GetConverter(null!);
        }, "typeToConvert");
    }
}
