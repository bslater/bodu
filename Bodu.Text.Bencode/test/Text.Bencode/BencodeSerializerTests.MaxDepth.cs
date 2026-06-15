// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.MaxDepth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="BencodeSerializerOptions.MaxDepth" /> bounds the nesting the serializer accepts on both
/// sides of the round trip: a graph deeper than the limit throws when writing and when reading, a graph within the limit
/// succeeds, and the property itself rejects a negative value and treats zero as the default.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that serializing a POCO graph nested deeper than <see cref="BencodeSerializerOptions.MaxDepth" /> throws
    /// <see cref="BencodeSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGraphExceedsMaxDepth_ShouldThrowBencodeSerializationException()
    {
        var options = new BencodeSerializerOptions { MaxDepth = 2 };
        var deep = new RecursiveModel { Child = new RecursiveModel { Child = new RecursiveModel() } };

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Serialize(deep, options);
        });
    }

    /// <summary>
    /// Verifies that serializing an object graph nested beyond the absolute ceiling throws
    /// <see cref="BencodeSerializationException" /> even when the configured maximum depth is far larger, confirming the
    /// caller-supplied <see cref="BencodeSerializerOptions.MaxDepth" /> is clamped to the hard ceiling.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGraphExceedsAbsoluteCapDespiteLargeMaxDepth_ShouldThrowBencodeSerializationException()
    {
        var options = new BencodeSerializerOptions { MaxDepth = int.MaxValue };

        var deep = new RecursiveModel();
        for (var i = 0; i < BencodeLimits.AbsoluteMaxDepth + 1; i++)
            deep = new RecursiveModel { Child = deep };

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Serialize(deep, options);
        });
    }

    /// <summary>
    /// Verifies that serializing a graph nested far beyond the ceiling throws a catchable
    /// <see cref="BencodeSerializationException" /> rather than overflowing the call stack, even on a thread whose stack
    /// is deliberately constrained — pinning the requirement that the ceiling stays reachable before the physical stack
    /// is exhausted on a modest stack budget.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGraphFarExceedsCapOnConstrainedStack_ShouldThrowBencodeSerializationExceptionNotOverflow()
    {
        var options = new BencodeSerializerOptions { MaxDepth = int.MaxValue };

        var deep = new RecursiveModel();
        for (var i = 0; i < (BencodeLimits.AbsoluteMaxDepth * 32) + 1; i++)
            deep = new RecursiveModel { Child = deep };

        Exception? captured = null;
        var worker = new Thread(
            () =>
            {
                try
                {
                    _ = BencodeSerializer.Serialize(deep, options);
                }
                catch (Exception ex)
                {
                    captured = ex;
                }
            },
            maxStackSize: 256 << 10);

        worker.Start();
        worker.Join();

        Assert.IsNotNull(captured);
        Assert.AreEqual(typeof(BencodeSerializationException), captured.GetType());
    }

    /// <summary>
    /// Verifies that serializing lists nested far beyond the ceiling on a constrained stack throws a catchable
    /// <see cref="BencodeSerializationException" /> rather than overflowing, confirming the collection converter detects
    /// the ceiling cooperatively and unwinds through returns before the deep frames are entered.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNestedListsFarExceedCapOnConstrainedStack_ShouldThrowBencodeSerializationExceptionNotOverflow()
    {
        var options = new BencodeSerializerOptions { MaxDepth = int.MaxValue };

        var inner = new List<object>();
        var current = inner;
        for (var i = 0; i < (BencodeLimits.AbsoluteMaxDepth * 32) + 1; i++)
        {
            var next = new List<object>();
            current.Add(next);
            current = next;
        }

        var captured = RunOnConstrainedStack(() =>
        {
            _ = BencodeSerializer.Serialize(inner, options);
        });

        Assert.IsNotNull(captured);
        Assert.AreEqual(typeof(BencodeSerializationException), captured.GetType());
    }

    /// <summary>
    /// Verifies that serializing dictionaries nested far beyond the ceiling on a constrained stack throws a catchable
    /// <see cref="BencodeSerializationException" /> rather than overflowing, confirming the dictionary converter detects
    /// the ceiling cooperatively and unwinds through returns before the deep frames are entered.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNestedDictionariesFarExceedCapOnConstrainedStack_ShouldThrowBencodeSerializationExceptionNotOverflow()
    {
        var options = new BencodeSerializerOptions { MaxDepth = int.MaxValue };

        var root = new Dictionary<string, object>();
        var current = root;
        for (var i = 0; i < (BencodeLimits.AbsoluteMaxDepth * 32) + 1; i++)
        {
            var next = new Dictionary<string, object>();
            current["child"] = next;
            current = next;
        }

        var captured = RunOnConstrainedStack(() =>
        {
            _ = BencodeSerializer.Serialize(root, options);
        });

        Assert.IsNotNull(captured);
        Assert.AreEqual(typeof(BencodeSerializationException), captured.GetType());
    }

    /// <summary>
    /// Verifies that serializing a POCO graph nested no deeper than <see cref="BencodeSerializerOptions.MaxDepth" />
    /// succeeds and emits canonical bytes.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGraphWithinMaxDepth_ShouldSucceed()
    {
        var options = new BencodeSerializerOptions { MaxDepth = 2 };
        var shallow = new RecursiveModel { Child = new RecursiveModel() };

        var bytes = BencodeSerializer.Serialize(shallow, options);

        Assert.AreEqual("d5:Childdee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that deserializing Bencode nested deeper than <see cref="BencodeSerializerOptions.MaxDepth" /> throws
    /// <see cref="BencodeFormatException" />, surfaced from the reader.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenInputExceedsMaxDepth_ShouldThrowBencodeFormatException()
    {
        var options = new BencodeSerializerOptions { MaxDepth = 2 };
        var bytes = Encoding.Latin1.GetBytes("d5:Childd5:Childdeeee");

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BencodeSerializer.Deserialize<RecursiveModel>(bytes, options);
        });
    }

    /// <summary>
    /// Verifies that deserializing Bencode nested no deeper than <see cref="BencodeSerializerOptions.MaxDepth" />
    /// succeeds.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenInputWithinMaxDepth_ShouldSucceed()
    {
        var options = new BencodeSerializerOptions { MaxDepth = 2 };
        var bytes = Encoding.Latin1.GetBytes("d5:Childdee");

        var model = BencodeSerializer.Deserialize<RecursiveModel>(bytes, options);

        Assert.IsNotNull(model.Child);
        Assert.IsNull(model.Child.Child);
    }

    /// <summary>
    /// Verifies that setting <see cref="BencodeSerializerOptions.MaxDepth" /> to a negative value throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName</c> <c>value</c>.
    /// </summary>
    [TestMethod]
    public void MaxDepth_WhenSetToNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new BencodeSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            options.MaxDepth = -1;
        }, "value");
    }

    /// <summary>
    /// Verifies that setting <see cref="BencodeSerializerOptions.MaxDepth" /> to zero resets it to
    /// <see cref="BencodeSerializerOptions.DefaultMaxDepth" /> rather than disabling depth tracking.
    /// </summary>
    [TestMethod]
    public void MaxDepth_WhenSetToZero_ShouldResetToDefault()
    {
        var options = new BencodeSerializerOptions { MaxDepth = 0 };

        Assert.AreEqual(BencodeSerializerOptions.DefaultMaxDepth, options.MaxDepth);
    }

    /// <summary>
    /// Verifies that setting <see cref="BencodeSerializerOptions.MaxDepth" /> after the options have been used to
    /// serialize throws <see cref="InvalidOperationException" />, because the options are then read-only.
    /// </summary>
    [TestMethod]
    public void MaxDepth_WhenSetAfterOptionsReadOnly_ShouldThrowInvalidOperationException()
    {
        var options = new BencodeSerializerOptions();
        _ = BencodeSerializer.Serialize(new RecursiveModel(), options);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            options.MaxDepth = 8;
        });
    }

    /// <summary>
    /// Verifies that deserializing a document nested beyond the absolute ceiling throws
    /// <see cref="BencodeFormatException" /> even when the configured maximum depth is far larger, confirming the reader
    /// clamps the caller-supplied <see cref="BencodeSerializerOptions.MaxDepth" /> to the hard ceiling.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDocumentExceedsAbsoluteCapDespiteLargeMaxDepth_ShouldThrowBencodeFormatException()
    {
        var options = new BencodeSerializerOptions { MaxDepth = int.MaxValue };
        var bytes = BuildNestedDictionaryDocument(BencodeLimits.AbsoluteMaxDepth + 1);

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BencodeSerializer.Deserialize<RecursiveModel>(bytes, options);
        });
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeSerializerOptions.MaxDepth" /> larger than the absolute ceiling is still
    /// accepted by the property, confirming the ceiling is enforced while writing or reading rather than at
    /// configuration time.
    /// </summary>
    [TestMethod]
    public void MaxDepth_WhenSetAboveAbsoluteCap_ShouldBeAccepted()
    {
        var options = new BencodeSerializerOptions { MaxDepth = int.MaxValue };

        Assert.AreEqual(int.MaxValue, options.MaxDepth);
    }

    /// <summary>
    /// Verifies that deserializing a document nested far beyond the ceiling on a constrained stack throws a catchable
    /// <see cref="BencodeFormatException" /> rather than overflowing the call stack, confirming the reader's iterative
    /// depth cap stops the binding recursion before the physical stack is exhausted on a modest stack budget.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDocumentFarExceedsCapOnConstrainedStack_ShouldThrowBencodeFormatExceptionNotOverflow()
    {
        var options = new BencodeSerializerOptions { MaxDepth = int.MaxValue };
        var bytes = BuildNestedDictionaryDocument((BencodeLimits.AbsoluteMaxDepth * 32) + 1);

        var captured = RunOnConstrainedStack(() =>
        {
            _ = BencodeSerializer.Deserialize<RecursiveModel>(bytes, options);
        });

        Assert.IsNotNull(captured);
        Assert.AreEqual(typeof(BencodeFormatException), captured.GetType());
    }

    /// <summary>
    /// Runs <paramref name="work" /> on a deliberately constrained 256 KB thread stack, returning the exception it threw
    /// or <see langword="null" /> when it completed. A process-terminating <see cref="StackOverflowException" /> cannot
    /// be captured, so a non-null catchable result confirms the operation stayed within the stack budget.
    /// </summary>
    /// <param name="work">The operation to run.</param>
    /// <returns>The captured exception, or <see langword="null" /> when the operation completed.</returns>
    private static Exception? RunOnConstrainedStack(Action work)
    {
        Exception? captured = null;
        var worker = new Thread(
            () =>
            {
                try
                {
                    work();
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
    /// Builds a Bencode document of <paramref name="depth" /> nested dictionaries under a single recurring <c>Child</c>
    /// key, used to drive the reader and binder to a controlled nesting depth.
    /// </summary>
    /// <param name="depth">The number of nested dictionaries to emit.</param>
    /// <returns>The Bencode source bytes.</returns>
    private static byte[] BuildNestedDictionaryDocument(int depth)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < depth - 1; i++)
            builder.Append("d5:Child");
        builder.Append("de");
        for (var i = 0; i < depth - 1; i++)
            builder.Append('e');

        return Encoding.Latin1.GetBytes(builder.ToString());
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
}
