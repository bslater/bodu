// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.MaxDepth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="TomlSerializerOptions.MaxDepth" /> bounds the nesting the serializer accepts on both sides
/// of the round trip: a graph deeper than the limit throws when writing, value nesting deeper than the limit throws
/// when reading, a graph within the limit succeeds, and the property itself rejects a negative value, treats zero as
/// the default, and is frozen once the options have been used.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that serializing a POCO graph nested deeper than <see cref="TomlSerializerOptions.MaxDepth" /> throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGraphExceedsMaxDepth_ShouldThrowTomlSerializationException()
    {
        var options = new TomlSerializerOptions { MaxDepth = 2 };
        var deep = new RecursiveModel { Child = new RecursiveModel { Child = new RecursiveModel() } };

        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(deep, options);
        });
    }

    /// <summary>
    /// Verifies that serializing a POCO graph nested no deeper than <see cref="TomlSerializerOptions.MaxDepth" />
    /// succeeds, emitting the empty nested table as a <c>[Child]</c> header block.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGraphWithinMaxDepth_ShouldSucceed()
    {
        var options = new TomlSerializerOptions { MaxDepth = 2 };
        var shallow = new RecursiveModel { Child = new RecursiveModel() };

        string text = TomlSerializer.Serialize(shallow, options);

        Assert.AreEqual("[Child]\n", text);
    }

    /// <summary>
    /// Verifies that deserializing TOML whose inline-table value nesting exceeds
    /// <see cref="TomlSerializerOptions.MaxDepth" /> throws <see cref="TomlFormatException" />, surfaced from the
    /// reader.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenInlineTableNestingExceedsMaxDepth_ShouldThrowTomlFormatException()
    {
        var options = new TomlSerializerOptions { MaxDepth = 2 };

        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = TomlSerializer.Deserialize<RecursiveModel>("Child = { Child = { Child = {} } }\n", options);
        });
    }

    /// <summary>
    /// Verifies that deserializing TOML whose array value nesting exceeds
    /// <see cref="TomlSerializerOptions.MaxDepth" /> throws <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenArrayNestingExceedsMaxDepth_ShouldThrowTomlFormatException()
    {
        var options = new TomlSerializerOptions { MaxDepth = 2 };

        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = TomlSerializer.Deserialize<NestedArrayDepthModel>("A = [[[1]]]\n", options);
        });
    }

    /// <summary>
    /// Verifies that deserializing TOML whose value nesting stays within
    /// <see cref="TomlSerializerOptions.MaxDepth" /> succeeds.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenInlineTableNestingWithinMaxDepth_ShouldSucceed()
    {
        var options = new TomlSerializerOptions { MaxDepth = 2 };

        var model = TomlSerializer.Deserialize<RecursiveModel>("Child = { Child = {} }\n", options);

        Assert.IsNotNull(model.Child);
        Assert.IsNotNull(model.Child.Child);
        Assert.IsNull(model.Child.Child.Child);
    }

    /// <summary>
    /// Verifies that tables defined through <c>[dotted.path]</c> header blocks are not bounded by
    /// <see cref="TomlSerializerOptions.MaxDepth" /> on read, pinning the current reader behavior in which the limit
    /// applies to value nesting (inline tables and arrays) rather than to header-defined table paths.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenHeaderTablePathExceedsMaxDepth_ShouldNotThrow()
    {
        var options = new TomlSerializerOptions { MaxDepth = 2 };

        var model = TomlSerializer.Deserialize<RecursiveModel>("[Child]\n[Child.Child]\n", options);

        Assert.IsNotNull(model.Child);
        Assert.IsNotNull(model.Child.Child);
    }

    /// <summary>
    /// Verifies that setting <see cref="TomlSerializerOptions.MaxDepth" /> to a negative value throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName</c> <c>value</c>.
    /// </summary>
    [TestMethod]
    public void MaxDepth_WhenSetToNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new TomlSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            options.MaxDepth = -1;
        }, "value");
    }

    /// <summary>
    /// Verifies that setting <see cref="TomlSerializerOptions.MaxDepth" /> to zero resets it to
    /// <see cref="TomlSerializerOptions.DefaultMaxDepth" /> rather than disabling depth tracking.
    /// </summary>
    [TestMethod]
    public void MaxDepth_WhenSetToZero_ShouldResetToDefault()
    {
        var options = new TomlSerializerOptions { MaxDepth = 0 };

        Assert.AreEqual(TomlSerializerOptions.DefaultMaxDepth, options.MaxDepth);
    }

    /// <summary>
    /// Verifies that setting <see cref="TomlSerializerOptions.MaxDepth" /> after the options have been used to
    /// serialize throws <see cref="InvalidOperationException" />, because the options are then read-only.
    /// </summary>
    [TestMethod]
    public void MaxDepth_WhenSetAfterOptionsReadOnly_ShouldThrowInvalidOperationException()
    {
        var options = new TomlSerializerOptions();
        _ = TomlSerializer.Serialize(new RecursiveModel(), options);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            options.MaxDepth = 8;
        });
    }

    /// <summary>
    /// A self-referential model used to build graphs of arbitrary nesting depth.
    /// </summary>
    private sealed class RecursiveModel
    {
        /// <summary>
        /// Gets or sets the nested child, omitted from the output when <see langword="null" />.
        /// </summary>
        /// <returns>The child, or <see langword="null" />.</returns>
        public RecursiveModel? Child { get; set; }
    }

    /// <summary>
    /// A model with a doubly nested integer list used to exercise array nesting depth on read.
    /// </summary>
    private sealed class NestedArrayDepthModel
    {
        /// <summary>
        /// Gets or sets the nested list, read from a TOML array of arrays.
        /// </summary>
        /// <returns>The nested list, or <see langword="null" />.</returns>
        public List<List<List<int>>>? A { get; set; }
    }
}
