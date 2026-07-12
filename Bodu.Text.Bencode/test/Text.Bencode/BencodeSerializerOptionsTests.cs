// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Serialization;
using Bodu.Text.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the parameter-validation contracts of <see cref="BencodeSerializerOptions" />: undefined enumeration
/// values are rejected with the expected <c>ParamName</c>, converter resolution rejects a <see langword="null" />
/// type, and the converter list's tolerance of <see langword="null" /> entries is pinned.
/// </summary>
[TestClass]
public class BencodeSerializerOptionsTests
{
    /// <summary>
    /// Verifies that setting <see cref="BencodeSerializerOptions.DefaultIgnoreCondition" /> to an undefined
    /// <see cref="IgnoreCondition" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>value</c>.
    /// </summary>
    [TestMethod]
    public void DefaultIgnoreCondition_WhenSetToUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new BencodeSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            options.DefaultIgnoreCondition = (IgnoreCondition)99;
        }, "value");
    }

    /// <summary>
    /// Verifies that setting <see cref="BencodeSerializerOptions.UnmappedMemberHandling" /> to an undefined
    /// <see cref="UnmappedMemberHandling" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>value</c>.
    /// </summary>
    [TestMethod]
    public void UnmappedMemberHandling_WhenSetToUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new BencodeSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            options.UnmappedMemberHandling = (UnmappedMemberHandling)99;
        }, "value");
    }

    /// <summary>
    /// Verifies that setting <see cref="BencodeSerializerOptions.PreferredObjectCreationHandling" /> to an undefined
    /// <see cref="ObjectCreationHandling" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>value</c>.
    /// </summary>
    [TestMethod]
    public void PreferredObjectCreationHandling_WhenSetToUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new BencodeSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            options.PreferredObjectCreationHandling = (ObjectCreationHandling)99;
        }, "value");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializerOptions.GetConverter(Type)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>typeToConvert</c> when the type is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetConverter_WhenTypeToConvertNull_ShouldThrowArgumentNullException()
    {
        var options = new BencodeSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = options.GetConverter(null!);
        }, "typeToConvert");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializerOptions.Converters" /> accepts a <see langword="null" /> entry
    /// without throwing, pinning the current list-backed behavior.
    /// </summary>
    [TestMethod]
    public void Converters_WhenNullAdded_ShouldAcceptEntry()
    {
        var options = new BencodeSerializerOptions();

        options.Converters.Add(null!);

        Assert.HasCount(1, options.Converters);
        Assert.IsNull(options.Converters[0]);
    }
}
