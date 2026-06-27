// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.MaxDepth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bodu.Test.Assertions;
using Bodu.Test.IO;
using Bodu.Test.Kat;
using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Nodes;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

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

        RecursiveModel model = TomlSerializer.Deserialize<RecursiveModel>("Child = { Child = {} }\n", options);

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

        RecursiveModel model = TomlSerializer.Deserialize<RecursiveModel>("[Child]\n[Child.Child]\n", options);

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
        /// <value>The child, or <see langword="null" />.</value>
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
        /// <value>The nested list, or <see langword="null" />.</value>
        public List<List<List<int>>>? A { get; set; }
    }
    /// <summary>
    /// Verifies that serializing an object graph nested beyond the absolute ceiling throws
    /// <see cref="TomlSerializationException" /> even when the configured maximum depth is far larger, confirming the
    /// caller-supplied <see cref="TomlSerializerOptions.MaxDepth" /> is clamped to the hard ceiling.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGraphExceedsAbsoluteCapDespiteLargeMaxDepth_ShouldThrowTomlSerializationException()
    {
        var options = new TomlSerializerOptions { MaxDepth = int.MaxValue };

        RecursiveModel deep = new();
        for (int i = 0; i < TomlLimits.AbsoluteMaxDepth + 1; i++)
            deep = new RecursiveModel { Child = deep };

        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(deep, options);
        });
    }

    /// <summary>
    /// Verifies that deserializing a document nested beyond the absolute ceiling throws <see cref="TomlFormatException" />
    /// even when the configured maximum depth is far larger, confirming the reader clamps the caller-supplied
    /// <see cref="TomlSerializerOptions.MaxDepth" /> to the hard ceiling.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDocumentExceedsAbsoluteCapDespiteLargeMaxDepth_ShouldThrowTomlFormatException()
    {
        var options = new TomlSerializerOptions { MaxDepth = int.MaxValue };
        string toml = BuildNestedInlineTableDocument(TomlLimits.AbsoluteMaxDepth + 1);

        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = TomlSerializer.Deserialize<RecursiveModel>(toml, options);
        });
    }

    /// <summary>
    /// Verifies that serializing a graph nested far beyond the ceiling throws a catchable
    /// <see cref="TomlSerializationException" /> rather than overflowing the call stack, even when serialization runs on
    /// a thread whose stack is deliberately constrained.
    /// </summary>
    /// <remarks>
    /// The serializer detects the ceiling cooperatively before opening the over-deep container and unwinds through
    /// normal returns, throwing once at the root. A graph many times deeper than the ceiling on an explicit 256 KB
    /// stack therefore never builds the deep frames: a regression to throwing from the deepest frame (which needs stack
    /// reserve to dispatch on a near-exhausted stack) would terminate the process with an uncatchable
    /// <see cref="StackOverflowException" />, so a catchable result here proves detection stays shallow.
    /// </remarks>
    [TestMethod]
    public void Serialize_WhenGraphFarExceedsCapOnConstrainedStack_ShouldThrowTomlSerializationExceptionNotOverflow()
    {
        var options = new TomlSerializerOptions { MaxDepth = int.MaxValue };

        RecursiveModel deep = new();
        for (int i = 0; i < (TomlLimits.AbsoluteMaxDepth * 32) + 1; i++)
            deep = new RecursiveModel { Child = deep };

        Exception? captured = SerializeOnConstrainedStack(deep, options);

        Assert.IsNotNull(captured);
        Assert.AreEqual(typeof(TomlSerializationException), captured.GetType());
    }

    /// <summary>
    /// Verifies that serializing dictionaries nested beyond the ceiling throws
    /// <see cref="TomlSerializationException" />, confirming the depth guard covers the dictionary converter path and not
    /// only object property chains.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNestedDictionariesExceedCap_ShouldThrowTomlSerializationException()
    {
        var options = new TomlSerializerOptions { MaxDepth = int.MaxValue };

        var root = new Dictionary<string, object>();
        Dictionary<string, object> current = root;
        for (int i = 0; i < TomlLimits.AbsoluteMaxDepth + 2; i++)
        {
            var next = new Dictionary<string, object>();
            current["child"] = next;
            current = next;
        }

        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(root, options);
        });
    }

    /// <summary>
    /// Verifies that serializing arrays nested beyond the ceiling throws <see cref="TomlSerializationException" />,
    /// confirming the depth guard covers the collection converter path and not only object property chains.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNestedArraysExceedCap_ShouldThrowTomlSerializationException()
    {
        var options = new TomlSerializerOptions { MaxDepth = int.MaxValue };

        var inner = new List<object>();
        List<object> current = inner;
        for (int i = 0; i < TomlLimits.AbsoluteMaxDepth + 2; i++)
        {
            var next = new List<object>();
            current.Add(next);
            current = next;
        }

        var root = new Dictionary<string, object> { ["values"] = inner };

        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(root, options);
        });
    }

    /// <summary>
    /// Verifies that serializing arrays nested far beyond the ceiling on a constrained stack throws a catchable
    /// <see cref="TomlSerializationException" /> rather than overflowing, confirming the collection converter detects the
    /// ceiling cooperatively and unwinds through returns before the deep frames are entered.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNestedArraysFarExceedCapOnConstrainedStack_ShouldThrowTomlSerializationExceptionNotOverflow()
    {
        var options = new TomlSerializerOptions { MaxDepth = int.MaxValue };

        var inner = new List<object>();
        List<object> current = inner;
        for (int i = 0; i < (TomlLimits.AbsoluteMaxDepth * 32) + 1; i++)
        {
            var next = new List<object>();
            current.Add(next);
            current = next;
        }

        var root = new Dictionary<string, object> { ["values"] = inner };

        Exception? captured = SerializeOnConstrainedStack(root, options);

        Assert.IsNotNull(captured);
        Assert.AreEqual(typeof(TomlSerializationException), captured.GetType());
    }

    /// <summary>
    /// Verifies that serializing dictionaries nested far beyond the ceiling on a constrained stack throws a catchable
    /// <see cref="TomlSerializationException" /> rather than overflowing, confirming the dictionary converter detects the
    /// ceiling cooperatively and unwinds through returns before the deep frames are entered.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNestedDictionariesFarExceedCapOnConstrainedStack_ShouldThrowTomlSerializationExceptionNotOverflow()
    {
        var options = new TomlSerializerOptions { MaxDepth = int.MaxValue };

        var root = new Dictionary<string, object>();
        Dictionary<string, object> current = root;
        for (int i = 0; i < (TomlLimits.AbsoluteMaxDepth * 32) + 1; i++)
        {
            var next = new Dictionary<string, object>();
            current["child"] = next;
            current = next;
        }

        Exception? captured = SerializeOnConstrainedStack(root, options);

        Assert.IsNotNull(captured);
        Assert.AreEqual(typeof(TomlSerializationException), captured.GetType());
    }

    /// <summary>
    /// Verifies that a <see cref="TomlSerializerOptions.MaxDepth" /> larger than the absolute ceiling is still accepted
    /// by the property, confirming the ceiling is enforced while parsing or writing rather than at configuration time.
    /// </summary>
    [TestMethod]
    public void MaxDepth_WhenSetAboveAbsoluteCap_ShouldBeAccepted()
    {
        var options = new TomlSerializerOptions { MaxDepth = int.MaxValue };

        Assert.AreEqual(int.MaxValue, options.MaxDepth);
    }

    /// <summary>
    /// Serializes <paramref name="value" /> on a deliberately constrained 256 KB thread stack, returning the exception
    /// it threw, or <see langword="null" /> when it completed. A process-terminating
    /// <see cref="StackOverflowException" /> cannot be captured, so a non-null catchable result confirms the serializer
    /// stayed within the stack budget.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The captured exception, or <see langword="null" /> when serialization completed.</returns>
    private static Exception? SerializeOnConstrainedStack<T>(T value, TomlSerializerOptions options)
    {
        Exception? captured = null;
        var worker = new Thread(
            () =>
            {
                try
                {
                    _ = TomlSerializer.Serialize(value, options);
                }
                catch (Exception ex)
                {
                    captured = ex;
                }
            },
            maxStackSize: 256 << 10);

        worker.Start();
        worker.Join();

        return captured;
    }

    /// <summary>
    /// Builds a TOML document of <paramref name="depth" /> nested inline tables under a single recurring key, used to
    /// drive the parser to a controlled nesting depth.
    /// </summary>
    /// <param name="depth">The number of nested inline tables to emit.</param>
    /// <returns>The TOML source text.</returns>
    private static string BuildNestedInlineTableDocument(int depth)
    {
        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < depth; i++)
            builder.Append("Child = { ");
        for (int i = 0; i < depth; i++)
            builder.Append('}');
        builder.Append('\n');

        return builder.ToString();
    }

}
