// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerOptionsTests.Converters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the read-only and null-rejection contract of <see cref="TomlSerializerOptions.Converters" />: a
/// <see langword="null" /> converter is rejected at insertion, the collection rejects every mutation once the options
/// have become read-only, and reads remain available after freezing.
/// </summary>
public partial class TomlSerializerOptionsTests
{
    /// <summary>
    /// Verifies that adding a <see langword="null" /> converter to mutable options throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>item</c>.
    /// </summary>
    [TestMethod]
    public void Converters_WhenNullAddedToMutableOptions_ShouldThrowArgumentNullException()
    {
        var options = new TomlSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            options.Converters.Add(null!);
        }, "item");
    }

    /// <summary>
    /// Verifies that inserting a <see langword="null" /> converter into mutable options throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>item</c>.
    /// </summary>
    [TestMethod]
    public void Converters_WhenNullInsertedIntoMutableOptions_ShouldThrowArgumentNullException()
    {
        var options = new TomlSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            options.Converters.Insert(0, null!);
        }, "item");
    }

    /// <summary>
    /// Verifies that assigning a <see langword="null" /> converter through the list indexer throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>value</c>.
    /// </summary>
    [TestMethod]
    public void Converters_WhenNullAssignedThroughIndexer_ShouldThrowArgumentNullException()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter());

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            options.Converters[0] = null!;
        }, "value");
    }

    /// <summary>
    /// Verifies that adding a converter after the options have become read-only throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Converters_WhenAddedAfterMakeReadOnly_ShouldThrowInvalidOperationException()
    {
        var options = new TomlSerializerOptions();
        options.MakeReadOnly();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            options.Converters.Add(new TomlStringEnumConverter());
        });
    }

    /// <summary>
    /// Verifies that inserting a converter after the options have become read-only throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Converters_WhenInsertedAfterMakeReadOnly_ShouldThrowInvalidOperationException()
    {
        var options = new TomlSerializerOptions();
        options.MakeReadOnly();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            options.Converters.Insert(0, new TomlStringEnumConverter());
        });
    }

    /// <summary>
    /// Verifies that clearing the converter list after the options have become read-only throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Converters_WhenClearedAfterMakeReadOnly_ShouldThrowInvalidOperationException()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter());
        options.MakeReadOnly();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            options.Converters.Clear();
        });
    }

    /// <summary>
    /// Verifies that removing a converter after the options have become read-only throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Converters_WhenRemovedAfterMakeReadOnly_ShouldThrowInvalidOperationException()
    {
        var options = new TomlSerializerOptions();
        var converter = new TomlStringEnumConverter();
        options.Converters.Add(converter);
        options.MakeReadOnly();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = options.Converters.Remove(converter);
        });
    }

    /// <summary>
    /// Verifies that replacing a converter through the list indexer after the options have become read-only throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Converters_WhenItemReplacedAfterMakeReadOnly_ShouldThrowInvalidOperationException()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter());
        options.MakeReadOnly();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            options.Converters[0] = new TomlStringEnumConverter();
        });
    }

    /// <summary>
    /// Verifies that the converter list remains readable after the options have become read-only, so freezing
    /// restricts mutation only.
    /// </summary>
    [TestMethod]
    public void Converters_WhenReadAfterMakeReadOnly_ShouldStillEnumerate()
    {
        var options = new TomlSerializerOptions();
        var converter = new TomlStringEnumConverter();
        options.Converters.Add(converter);
        options.MakeReadOnly();

        Assert.HasCount(1, options.Converters);
        Assert.AreSame(converter, options.Converters[0]);
        Assert.Contains(converter, options.Converters);

        var enumerated = new List<TomlConverter>();
        foreach (TomlConverter item in options.Converters)
            enumerated.Add(item);

        Assert.HasCount(1, enumerated);
    }
}
