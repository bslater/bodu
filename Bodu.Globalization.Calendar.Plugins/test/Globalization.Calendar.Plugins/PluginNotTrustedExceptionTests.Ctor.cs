// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginNotTrustedExceptionTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class PluginNotTrustedExceptionTests
{
    /// <summary>
    /// Verifies that the message, assembly-name and reason constructor exposes all three and no inner exception.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageAssemblyNameAndReason_ShouldExposeAll()
    {
        PluginNotTrustedException ex = new("rejected", "plugin.asm", "untrusted hash");

        Assert.AreEqual("rejected", ex.Message);
        Assert.AreEqual("plugin.asm", ex.AssemblyName);
        Assert.AreEqual("untrusted hash", ex.Reason);
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that the message, assembly-name and reason constructor accepts a <see langword="null" /> reason and
    /// exposes it as such.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenReasonIsNull_ShouldExposeNullReason()
    {
        PluginNotTrustedException ex = new("rejected", "plugin.asm", null);

        Assert.AreEqual("plugin.asm", ex.AssemblyName);
        Assert.IsNull(ex.Reason);
    }

    /// <summary>
    /// Verifies that the message-only constructor exposes the message and leaves the assembly name, the reason and
    /// the inner exception unset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageOnly_ShouldExposeMessageAndNullProperties()
    {
        PluginNotTrustedException ex = new("rejected");

        Assert.AreEqual("rejected", ex.Message);
        Assert.IsNull(ex.AssemblyName);
        Assert.IsNull(ex.Reason);
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that the message-and-inner constructor exposes both and leaves the assembly name and the reason unset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageAndInner_ShouldExposeBothAndNullProperties()
    {
        InvalidOperationException inner = new("root");

        PluginNotTrustedException ex = new("rejected", inner);

        Assert.AreEqual("rejected", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
        Assert.IsNull(ex.AssemblyName);
        Assert.IsNull(ex.Reason);
    }

    /// <summary>
    /// Verifies that the parameterless constructor produces an exception with a default message and nothing else set.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldProduceExceptionWithDefaultMessage()
    {
        PluginNotTrustedException ex = new();

        Assert.IsFalse(string.IsNullOrEmpty(ex.Message));
        Assert.IsNull(ex.AssemblyName);
        Assert.IsNull(ex.Reason);
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that the exception derives from <see cref="NotableDatePluginException" />, so a caller catching the
    /// plugin exception family catches a trust rejection too.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructed_ShouldDeriveFromNotableDatePluginException()
    {
        PluginNotTrustedException ex = new("rejected");

        Assert.IsInstanceOfType<NotableDatePluginException>(ex);
    }
}
