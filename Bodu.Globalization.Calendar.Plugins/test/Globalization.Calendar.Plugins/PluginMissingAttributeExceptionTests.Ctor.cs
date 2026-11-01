// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginMissingAttributeExceptionTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class PluginMissingAttributeExceptionTests
{
    /// <summary>
    /// Verifies that the message-and-assembly-name constructor exposes both and no inner exception.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageAndAssemblyName_ShouldExposeBoth()
    {
        PluginMissingAttributeException ex = new("missing attribute", "plugin.asm");

        Assert.AreEqual("missing attribute", ex.Message);
        Assert.AreEqual("plugin.asm", ex.AssemblyName);
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that the message-only constructor exposes the message and leaves the assembly name and the inner
    /// exception unset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageOnly_ShouldExposeMessageAndNullAssemblyName()
    {
        PluginMissingAttributeException ex = new("missing attribute");

        Assert.AreEqual("missing attribute", ex.Message);
        Assert.IsNull(ex.AssemblyName);
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that the message-and-inner constructor exposes both and leaves the assembly name unset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageAndInner_ShouldExposeBothAndNullAssemblyName()
    {
        InvalidOperationException inner = new("root");

        PluginMissingAttributeException ex = new("missing attribute", inner);

        Assert.AreEqual("missing attribute", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
        Assert.IsNull(ex.AssemblyName);
    }

    /// <summary>
    /// Verifies that the parameterless constructor produces an exception with a default message and nothing else set.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldProduceExceptionWithDefaultMessage()
    {
        PluginMissingAttributeException ex = new();

        Assert.IsFalse(string.IsNullOrEmpty(ex.Message));
        Assert.IsNull(ex.AssemblyName);
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that the exception derives from <see cref="NotableDatePluginException" />, so a caller catching the
    /// plugin exception family catches a missing attribute too.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructed_ShouldDeriveFromNotableDatePluginException()
    {
        PluginMissingAttributeException ex = new("missing attribute");

        Assert.IsInstanceOfType<NotableDatePluginException>(ex);
    }
}
