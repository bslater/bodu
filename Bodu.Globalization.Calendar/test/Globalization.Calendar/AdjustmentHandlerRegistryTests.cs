// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentHandlerRegistryTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies registration, lookup, and argument-validation contracts of
/// <see cref="AdjustmentHandlerRegistry" />.
/// </summary>
[TestClass]
public sealed class AdjustmentHandlerRegistryTests
{
    /// <summary>
    /// Verifies that the parameterless constructor yields an empty, functional registry.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenParameterless_ShouldReturnEmptyRegistry()
    {
        var registry = new AdjustmentHandlerRegistry();

        bool found = registry.TryGet("any-key", out IAdjustmentHandler handler);

        Assert.IsFalse(found);
        Assert.IsNull(handler);
    }

    /// <summary>
    /// Verifies that the seeded constructor registers every supplied pair.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSeeded_ShouldRegisterAllSuppliedHandlers()
    {
        var handlerA = new StubHandler();
        var handlerB = new StubHandler();

        var registry = new AdjustmentHandlerRegistry(new[]
        {
            new KeyValuePair<string, IAdjustmentHandler>("a", handlerA),
            new KeyValuePair<string, IAdjustmentHandler>("b", handlerB),
        });

        Assert.IsTrue(registry.TryGet("a", out var resolvedA));
        Assert.AreSame(handlerA, resolvedA);
        Assert.IsTrue(registry.TryGet("b", out var resolvedB));
        Assert.AreSame(handlerB, resolvedB);
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> seed collection throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenHandlersIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new AdjustmentHandlerRegistry(null!);
        });

        Assert.AreEqual("handlers", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentHandlerRegistry.Register(string, IAdjustmentHandler)" />
    /// rejects null, empty, or whitespace keys via <see cref="ArgumentException" />.
    /// </summary>
    [DataRow(null!)]
    [DataRow("")]
    [DataRow("   ")]
    [TestMethod]
    public void Register_WhenKeyIsNullOrWhitespace_ShouldThrowArgumentException(string? key)
    {
        var registry = new AdjustmentHandlerRegistry();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = registry.Register(key!, new StubHandler());
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentHandlerRegistry.Register(string, IAdjustmentHandler)" />
    /// rejects a <see langword="null" /> handler with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Register_WhenHandlerIsNull_ShouldThrowArgumentNullException()
    {
        var registry = new AdjustmentHandlerRegistry();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = registry.Register("key", null!);
        });

        Assert.AreEqual("handler", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the registry looks up keys case-insensitively.
    /// </summary>
    [DataRow("Key")]
    [DataRow("KEY")]
    [DataRow("key")]
    [DataRow("kEy")]
    [TestMethod]
    public void TryGet_WhenKeyMatchesCaseInsensitively_ShouldReturnHandler(string lookupKey)
    {
        var registry = new AdjustmentHandlerRegistry();
        var handler = new StubHandler();
        registry.Register("Key", handler);

        Assert.IsTrue(registry.TryGet(lookupKey, out var resolved));
        Assert.AreSame(handler, resolved);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentHandlerRegistry.TryGet(string, out IAdjustmentHandler)" />
    /// returns <see langword="false" /> when the key is null, empty, or whitespace.
    /// </summary>
    [DataRow(null!)]
    [DataRow("")]
    [DataRow("   ")]
    [TestMethod]
    public void TryGet_WhenKeyIsNullOrWhitespace_ShouldReturnFalse(string? key)
    {
        var registry = new AdjustmentHandlerRegistry();
        registry.Register("actual", new StubHandler());

        bool found = registry.TryGet(key!, out IAdjustmentHandler handler);

        Assert.IsFalse(found);
        Assert.IsNull(handler);
    }

    /// <summary>
    /// Verifies that registering under an existing key replaces the previous handler.
    /// </summary>
    [TestMethod]
    public void Register_WhenKeyAlreadyRegistered_ShouldReplaceHandler()
    {
        var registry = new AdjustmentHandlerRegistry();
        var original = new StubHandler();
        var replacement = new StubHandler();

        registry.Register("key", original).Register("key", replacement);

        Assert.IsTrue(registry.TryGet("key", out var resolved));
        Assert.AreSame(replacement, resolved);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentHandlerRegistry.Register(string, IAdjustmentHandler)" />
    /// returns the registry so callers can chain fluently.
    /// </summary>
    [TestMethod]
    public void Register_WhenCalled_ShouldReturnSameRegistryForChaining()
    {
        var registry = new AdjustmentHandlerRegistry();
        var handler = new StubHandler();

        AdjustmentHandlerRegistry returned = registry.Register("key", handler);

        Assert.AreSame(registry, returned);
    }

    /// <summary>
    /// Minimal <see cref="IAdjustmentHandler" /> test double. Never invoked by tests in this
    /// file — only its reference identity matters.
    /// </summary>
    private sealed class StubHandler
        : IAdjustmentHandler
    {
        public AdjustmentHandlerResult Apply(AdjustmentHandlerContext context) =>
            new AdjustmentHandlerResult(false, context.Date, null);
    }
}
