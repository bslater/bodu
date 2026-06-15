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
        for (var i = 0; i < TomlLimits.AbsoluteMaxDepth + 1; i++)
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
        var toml = BuildNestedInlineTableDocument(TomlLimits.AbsoluteMaxDepth + 1);

        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = TomlSerializer.Deserialize<RecursiveModel>(toml, options);
        });
    }

    /// <summary>
    /// Verifies that serializing a graph nested far beyond the ceiling throws a catchable
    /// <see cref="TomlSerializationException" /> rather than overflowing the call stack, even when serialization runs on
    /// a thread whose stack is deliberately constrained — pinning the requirement that the ceiling stays low enough to
    /// be reached before the physical stack is exhausted on a modest stack budget.
    /// </summary>
    /// <remarks>
    /// The serializer descends one native call-stack frame per nested container, so the absolute ceiling only converts
    /// unbounded nesting into a catchable failure while it is reached before the stack is exhausted. A ceiling raised
    /// above the stack budget would let the recursion overflow first, terminating the process with an uncatchable
    /// <see cref="StackOverflowException" /> that aborts the whole test run. Running on an explicit 256 KB stack with a
    /// graph many times deeper than the ceiling makes that regression observable on any platform: it throws here only
    /// because the ceiling is stack-safe.
    /// </remarks>
    [TestMethod]
    public void Serialize_WhenGraphFarExceedsCapOnConstrainedStack_ShouldThrowTomlSerializationExceptionNotOverflow()
    {
        var options = new TomlSerializerOptions { MaxDepth = int.MaxValue };

        RecursiveModel deep = new();
        for (var i = 0; i < (TomlLimits.AbsoluteMaxDepth * 32) + 1; i++)
            deep = new RecursiveModel { Child = deep };

        Exception? captured = null;
        var worker = new Thread(
            () =>
            {
                try
                {
                    _ = TomlSerializer.Serialize(deep, options);
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
    /// Builds a TOML document of <paramref name="depth" /> nested inline tables under a single recurring key, used to
    /// drive the parser to a controlled nesting depth.
    /// </summary>
    /// <param name="depth">The number of nested inline tables to emit.</param>
    /// <returns>The TOML source text.</returns>
    private static string BuildNestedInlineTableDocument(int depth)
    {
        var builder = new System.Text.StringBuilder();
        for (var i = 0; i < depth; i++)
            builder.Append("Child = { ");
        for (var i = 0; i < depth; i++)
            builder.Append('}');
        builder.Append('\n');

        return builder.ToString();
    }
}
