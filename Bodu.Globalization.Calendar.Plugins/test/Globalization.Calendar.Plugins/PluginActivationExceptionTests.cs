// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginActivationExceptionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Tests for <see cref="PluginActivationException" /> construction, which also exercises the base
/// <see cref="NotableDatePluginException" /> constructors.
/// </summary>
[TestClass]
public sealed class PluginActivationExceptionTests
{
    /// <summary>
    /// Verifies that the message-and-type constructor exposes both.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageAndType_ShouldExposeBoth()
    {
        PluginActivationException ex = new("boom", typeof(string));

        Assert.AreEqual("boom", ex.Message);
        Assert.AreEqual(typeof(string), ex.PluginType);
    }

    /// <summary>
    /// Verifies that the message, type, and inner-exception constructor exposes all three.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageTypeAndInner_ShouldExposeAll()
    {
        InvalidOperationException inner = new("root");

        PluginActivationException ex = new("boom", typeof(string), inner);

        Assert.AreEqual(typeof(string), ex.PluginType);
        Assert.AreSame(inner, ex.InnerException);
    }

    /// <summary>
    /// Verifies that the message-only constructor exposes the message with a <see langword="null" /> type.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageOnly_ShouldExposeMessageAndNullType()
    {
        PluginActivationException ex = new("boom");

        Assert.AreEqual("boom", ex.Message);
        Assert.IsNull(ex.PluginType);
    }

    /// <summary>
    /// Verifies that the message-and-inner constructor exposes both.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageAndInner_ShouldExposeBoth()
    {
        InvalidOperationException inner = new("root");

        PluginActivationException ex = new("boom", inner);

        Assert.AreEqual("boom", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
    }

    /// <summary>
    /// Verifies that the parameterless constructor produces an exception with a default message.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldProduceException()
    {
        PluginActivationException ex = new();

        Assert.IsNull(ex.PluginType);
        Assert.IsFalse(string.IsNullOrEmpty(ex.Message));
    }
}
