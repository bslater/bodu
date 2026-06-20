// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.DepthCap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that the serializer and parser enforce the hard absolute nesting ceiling that bounds call-stack use even
/// when the caller configures an arbitrarily large <see cref="TomlSerializerOptions.MaxDepth" />, so untrusted input
/// cannot drive either into a process-terminating <see cref="StackOverflowException" />.
/// </summary>
public partial class TomlSerializerTests
{
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
