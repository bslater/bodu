// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentHandlerRegistryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the <see cref="AdjustmentHandlerRegistry" /> and <see cref="AdjustmentTriggerHandlerRegistry" /> lookup
/// surfaces (<c>Contains</c> and <c>TryGet</c>) reflect what was registered.
/// </summary>
[TestClass]
public class AdjustmentHandlerRegistryTests
{
    /// <summary>
    /// A no-op adjustment handler used purely to populate the registry.
    /// </summary>
    private sealed class NoOpHandler : IAdjustmentHandler
    {
        /// <inheritdoc />
        public DateOnly? Adjust(AdjustmentHandlerContext context) => null;
    }

    /// <summary>
    /// A no-op trigger handler used purely to populate the registry.
    /// </summary>
    private sealed class NoOpTrigger : IAdjustmentTriggerHandler
    {
        /// <inheritdoc />
        public bool ShouldAdjust(AdjustmentTriggerContext context) => false;
    }

    /// <summary>
    /// Verifies that the adjustment-handler registry reports membership and resolves a registered handler, while
    /// reporting failure for an unknown key.
    /// </summary>
    [TestMethod]
    public void HandlerRegistry_ContainsAndTryGet_ShouldReflectRegistration()
    {
        var registry = new AdjustmentHandlerRegistry();
        var handler = new NoOpHandler();
        registry.Register("shift", handler);

        Assert.IsTrue(registry.Contains("shift"));
        Assert.IsFalse(registry.Contains("missing"));

        Assert.IsTrue(registry.TryGet("shift", out IAdjustmentHandler? resolved));
        Assert.AreSame(handler, resolved);

        Assert.IsFalse(registry.TryGet("missing", out IAdjustmentHandler? none));
        Assert.IsNull(none);
    }

    /// <summary>
    /// Verifies that the adjustment-trigger registry reports membership and resolves a registered trigger, while
    /// reporting failure for an unknown key.
    /// </summary>
    [TestMethod]
    public void TriggerRegistry_ContainsAndTryGet_ShouldReflectRegistration()
    {
        var registry = new AdjustmentTriggerHandlerRegistry();
        var trigger = new NoOpTrigger();
        registry.Register("cond", trigger);

        Assert.IsTrue(registry.Contains("cond"));
        Assert.IsFalse(registry.Contains("missing"));

        Assert.IsTrue(registry.TryGet("cond", out IAdjustmentTriggerHandler? resolved));
        Assert.AreSame(trigger, resolved);

        Assert.IsFalse(registry.TryGet("missing", out IAdjustmentTriggerHandler? none));
        Assert.IsNull(none);
    }
}
