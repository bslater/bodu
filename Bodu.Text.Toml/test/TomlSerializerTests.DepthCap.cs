// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.DepthCap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that the serializer enforces an absolute nesting ceiling that bounds resource use even when the caller
/// configures an arbitrarily large <see cref="TomlSerializerOptions.MaxDepth" />, so untrusted input cannot drive the
/// writer into a process-terminating <see cref="StackOverflowException" />.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that serializing an object graph nested beyond the absolute depth ceiling throws
    /// <see cref="TomlSerializationException" /> even when the configured maximum depth is far larger.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGraphExceedsAbsoluteCapDespiteLargeMaxDepth_ShouldThrowTomlSerializationException()
    {
        var options = new TomlSerializerOptions { MaxDepth = int.MaxValue };

        RecursiveModel deep = new();
        for (var i = 0; i < TomlLimits.AbsoluteMaxDepth + 2; i++)
            deep = new RecursiveModel { Child = deep };

        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(deep, options);
        });
    }

    /// <summary>
    /// Verifies that serializing an object graph nested beyond the absolute depth ceiling throws a catchable
    /// <see cref="TomlSerializationException" /> rather than overflowing the call stack, even when serialization runs
    /// on a thread whose stack is constrained — pinning the requirement that the ceiling stays safe on a modest stack
    /// budget rather than relying on the larger default stack of any particular platform.
    /// </summary>
    /// <remarks>
    /// The absolute ceiling exists to convert unbounded nesting into a catchable failure. That guarantee only holds
    /// when the ceiling is reached before the physical call stack is exhausted: a ceiling set above the stack budget
    /// lets recursion overflow first, terminating the process with an uncatchable <see cref="StackOverflowException" />
    /// that the default-stack <c>Serialize_WhenGraphExceedsAbsoluteCapDespiteLargeMaxDepth</c> test cannot observe
    /// where the platform stack is large. Running on an explicit, constrained stack reproduces that condition on any
    /// platform. The 512 KB budget sits well above what the bounded recursion requires yet well below what an
    /// unbounded ceiling would consume.
    /// </remarks>
    [TestMethod]
    public void Serialize_WhenGraphExceedsAbsoluteCapOnConstrainedStack_ShouldThrowTomlSerializationExceptionNotOverflow()
    {
        var options = new TomlSerializerOptions { MaxDepth = int.MaxValue };

        RecursiveModel deep = new();
        for (var i = 0; i < TomlLimits.AbsoluteMaxDepth + 2; i++)
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
            maxStackSize: 512 << 10);

        worker.Start();
        worker.Join();

        Assert.IsNotNull(captured);
        Assert.AreEqual(typeof(TomlSerializationException), captured.GetType());
    }

    /// <summary>
    /// Verifies that the serializer survives an over-deep graph even when the available stack is too small to reach the
    /// logical depth ceiling, by serializing on a stack that cannot hold <see cref="TomlLimits.AbsoluteMaxDepth" />
    /// levels of recursion and asserting a catchable <see cref="TomlSerializationException" /> is thrown rather than the
    /// process overflowing.
    /// </summary>
    /// <remarks>
    /// A 256 KB stack cannot hold <see cref="TomlLimits.AbsoluteMaxDepth" /> levels of serializer recursion, so the
    /// logical ceiling is unreachable on it: a guard expressed purely as a fixed level count set to the absolute maximum
    /// would overflow the stack before it could throw. Only a guard that measures the actual remaining stack on each
    /// descent converts the impending overflow into a catchable failure here, so this pins the runtime stack probe
    /// specifically, complementing the 512 KB test above where the logical ceiling may still be reached first on a
    /// platform with small stack frames.
    /// </remarks>
    [TestMethod]
    public void Serialize_WhenGraphExceedsStackBudgetBelowDepthCeiling_ShouldThrowTomlSerializationExceptionNotOverflow()
    {
        var options = new TomlSerializerOptions { MaxDepth = int.MaxValue };

        RecursiveModel deep = new();
        for (var i = 0; i < TomlLimits.AbsoluteMaxDepth + 2; i++)
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
}
