// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerOptionsTests.Converters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Text.Serialization;
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

        Assert.ThrowsExactly<InvalidOperationException>(options.Converters.Clear);
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

    /// <summary>
    /// Verifies that the converter list reports itself writable while the options are mutable, so a caller can test
    /// <see cref="ICollection{T}.IsReadOnly" /> before attempting a change.
    /// </summary>
    [TestMethod]
    public void Converters_WhenOptionsMutable_ShouldReportNotReadOnly()
    {
        var options = new TomlSerializerOptions();

        Assert.IsFalse(options.Converters.IsReadOnly);
    }

    /// <summary>
    /// Verifies that the converter list reports itself read-only once the options have become read-only, following the
    /// owning options rather than keeping a state of its own.
    /// </summary>
    [TestMethod]
    public void Converters_WhenOptionsReadOnly_ShouldReportReadOnly()
    {
        var options = new TomlSerializerOptions();
        options.MakeReadOnly();

        Assert.IsTrue(options.Converters.IsReadOnly);
    }

    /// <summary>
    /// Verifies that inserting a converter into mutable options places it at the given index, ahead of the converters
    /// already registered, so it is consulted before them.
    /// </summary>
    [TestMethod]
    public void Converters_WhenInsertedIntoMutableOptions_ShouldPlaceConverterAtIndex()
    {
        var options = new TomlSerializerOptions();
        var first = new TomlStringEnumConverter();
        var inserted = new TomlStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true);
        options.Converters.Add(first);

        options.Converters.Insert(0, inserted);

        Assert.HasCount(2, options.Converters);
        Assert.AreSame(inserted, options.Converters[0]);
        Assert.AreSame(first, options.Converters[1]);
    }

    /// <summary>
    /// Verifies that assigning a converter through the list indexer on mutable options replaces the converter at that
    /// index and leaves the count unchanged.
    /// </summary>
    [TestMethod]
    public void Converters_WhenItemReplacedThroughIndexerOnMutableOptions_ShouldStoreConverter()
    {
        var options = new TomlSerializerOptions();
        var replacement = new TomlStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true);
        options.Converters.Add(new TomlStringEnumConverter());

        options.Converters[0] = replacement;

        Assert.HasCount(1, options.Converters);
        Assert.AreSame(replacement, options.Converters[0]);
    }

    /// <summary>
    /// Verifies that removing a registered converter from mutable options takes it out of the list and reports the
    /// removal.
    /// </summary>
    [TestMethod]
    public void Converters_WhenRemovedFromMutableOptions_ShouldRemoveConverterAndReturnTrue()
    {
        var options = new TomlSerializerOptions();
        var converter = new TomlStringEnumConverter();
        options.Converters.Add(converter);

        bool removed = options.Converters.Remove(converter);

        Assert.IsTrue(removed);
        Assert.IsEmpty(options.Converters);
    }

    /// <summary>
    /// Verifies that removing a converter that was never registered leaves the list unchanged and reports that
    /// nothing was removed.
    /// </summary>
    [TestMethod]
    public void Converters_WhenRemovedConverterAbsent_ShouldReturnFalseAndKeepList()
    {
        var options = new TomlSerializerOptions();
        var registered = new TomlStringEnumConverter();
        options.Converters.Add(registered);

        bool removed = options.Converters.Remove(new TomlStringEnumConverter());

        Assert.IsFalse(removed);
        Assert.HasCount(1, options.Converters);
        Assert.AreSame(registered, options.Converters[0]);
    }

    /// <summary>
    /// Verifies that removing the converter at an index on mutable options takes out that converter only, closing the
    /// gap.
    /// </summary>
    [TestMethod]
    public void Converters_WhenRemovedAtOnMutableOptions_ShouldRemoveConverterAtIndex()
    {
        var options = new TomlSerializerOptions();
        var first = new TomlStringEnumConverter();
        var second = new TomlStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true);
        options.Converters.Add(first);
        options.Converters.Add(second);

        options.Converters.RemoveAt(0);

        Assert.HasCount(1, options.Converters);
        Assert.AreSame(second, options.Converters[0]);
    }

    /// <summary>
    /// Verifies that clearing the converter list on mutable options removes every registered converter.
    /// </summary>
    [TestMethod]
    public void Converters_WhenClearedOnMutableOptions_ShouldEmptyList()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter());
        options.Converters.Add(new TomlStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true));

        options.Converters.Clear();

        Assert.IsEmpty(options.Converters);
    }

    /// <summary>
    /// Verifies that <see cref="IList{T}.IndexOf" /> reports a registered converter's position and <c>-1</c> for a
    /// converter that is not registered.
    /// </summary>
    [TestMethod]
    public void Converters_WhenIndexOfQueried_ShouldReturnPositionOrMinusOne()
    {
        var options = new TomlSerializerOptions();
        var first = new TomlStringEnumConverter();
        var second = new TomlStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true);
        options.Converters.Add(first);
        options.Converters.Add(second);

        Assert.AreEqual(0, options.Converters.IndexOf(first));
        Assert.AreEqual(1, options.Converters.IndexOf(second));
        Assert.AreEqual(-1, options.Converters.IndexOf(new TomlStringEnumConverter()));
    }

    /// <summary>
    /// Verifies that enumerating the converter list through the non-generic <see cref="System.Collections.IEnumerable" />
    /// yields the registered converters in order, as the generic enumeration does.
    /// </summary>
    [TestMethod]
    public void Converters_WhenEnumeratedThroughNonGenericInterface_ShouldYieldConvertersInOrder()
    {
        var options = new TomlSerializerOptions();
        var first = new TomlStringEnumConverter();
        var second = new TomlStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true);
        options.Converters.Add(first);
        options.Converters.Add(second);

        var enumerated = new List<object?>();
        System.Collections.IEnumerator enumerator = ((System.Collections.IEnumerable)options.Converters).GetEnumerator();
        while (enumerator.MoveNext())
            enumerated.Add(enumerator.Current);

        CollectionAssert.AreEqual(new object[] { first, second }, enumerated);
    }
}
