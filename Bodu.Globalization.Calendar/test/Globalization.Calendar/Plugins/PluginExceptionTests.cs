// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginExceptionTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Verifies the public surface (constructors, properties, message formatting) of every plugin
/// exception type, since those paths are otherwise only indirectly reached through the
/// <see cref="ExternalPluginLoader" /> tests.
/// </summary>
[TestClass]
public sealed class PluginExceptionTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDatePluginException" /> exposes both the message-only and
    /// message-plus-inner-exception constructors.
    /// </summary>
    [TestMethod]
    public void NotableDatePluginException_WhenConstructed_ShouldExposeMessageAndInner()
    {
        var messageOnly = new NotableDatePluginException("just a message");
        var inner = new InvalidOperationException("root");
        var withInner = new NotableDatePluginException("wrapped", inner);

        Assert.AreEqual("just a message", messageOnly.Message);
        Assert.IsNull(messageOnly.InnerException);
        Assert.AreEqual("wrapped", withInner.Message);
        Assert.AreSame(inner, withInner.InnerException);
    }

    /// <summary>
    /// Verifies that <see cref="PluginActivationException" /> wraps the inner exception and
    /// exposes assembly path + plugin type.
    /// </summary>
    [TestMethod]
    public void PluginActivationException_ShouldExposeAssemblyPathAndPluginTypeAndWrapInner()
    {
        var inner = new InvalidOperationException("ctor failed");

        var ex = new PluginActivationException("/tmp/plugin.dll", typeof(string), inner);

        Assert.AreEqual("/tmp/plugin.dll", ex.AssemblyPath);
        Assert.AreEqual(typeof(string), ex.PluginType);
        Assert.AreSame(inner, ex.InnerException);
        Assert.IsTrue(ex.Message.Contains("String", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("/tmp/plugin.dll", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("ctor failed", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="PluginActivationException" /> copes with a null plugin type by
    /// substituting a placeholder in the formatted message.
    /// </summary>
    [TestMethod]
    public void PluginActivationException_WhenPluginTypeIsNull_ShouldUsePlaceholderInMessage()
    {
        var inner = new Exception("boom");

        var ex = new PluginActivationException("/tmp/plugin.dll", null, inner);

        Assert.IsNull(ex.PluginType);
        Assert.IsTrue(ex.Message.Contains("<unknown>", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="PluginNotTrustedException" /> exposes assembly path and reason,
    /// and that a null reason is rendered with the default placeholder.
    /// </summary>
    [DataRow("policy says no", "policy says no")]
    [DataRow(null!, "no reason supplied")]
    [TestMethod]
    public void PluginNotTrustedException_ShouldExposeAssemblyPathAndReason(string? reason, string expectedMessageFragment)
    {
        var ex = new PluginNotTrustedException("/tmp/plugin.dll", reason);

        Assert.AreEqual("/tmp/plugin.dll", ex.AssemblyPath);
        Assert.AreEqual(reason, ex.Reason);
        Assert.IsTrue(ex.Message.Contains(expectedMessageFragment, StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="PluginMissingAttributeException" /> exposes assembly path and
    /// reason and surfaces them in the formatted message.
    /// </summary>
    [TestMethod]
    public void PluginMissingAttributeException_ShouldExposeAssemblyPathAndReason()
    {
        var ex = new PluginMissingAttributeException("/tmp/plugin.dll", "attribute not found");

        Assert.AreEqual("/tmp/plugin.dll", ex.AssemblyPath);
        Assert.AreEqual("attribute not found", ex.Reason);
        Assert.IsTrue(ex.Message.Contains("attribute not found", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("/tmp/plugin.dll", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDatePluginAttribute" /> stores the plugin type and
    /// rejects a null type with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void NotableDatePluginAttribute_ShouldRoundTripPluginType_AndRejectNull()
    {
        var attribute = new NotableDatePluginAttribute(typeof(string));

        Assert.AreEqual(typeof(string), attribute.PluginType);
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new NotableDatePluginAttribute(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DelegatingPluginTrustPolicy" /> throws
    /// <see cref="ArgumentNullException" /> when the evaluator callback is null.
    /// </summary>
    [TestMethod]
    public void DelegatingPluginTrustPolicy_WhenEvaluatorIsNull_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DelegatingPluginTrustPolicy(null!);
        });

        Assert.AreEqual("evaluator", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the parameterless constructor on <see cref="NotableDatePluginException" /> produces an
    /// instance with the framework-default message and no inner exception.
    /// </summary>
    [TestMethod]
    public void NotableDatePluginException_WhenConstructedWithoutArguments_ShouldExposeDefaultMessageAndNoInner()
    {
        var ex = new NotableDatePluginException();

        Assert.IsFalse(string.IsNullOrEmpty(ex.Message));
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that the parameterless and single-string constructors on <see cref="PluginActivationException" />
    /// produce instances with the expected message and no inner exception, and that the message+inner
    /// constructor preserves both.
    /// </summary>
    [TestMethod]
    public void PluginActivationException_BasicCtors_ShouldExposeMessageAndInner()
    {
        var parameterless = new PluginActivationException();
        var messageOnly = new PluginActivationException("activation failed");
        var inner = new InvalidOperationException("root");
        var withInner = new PluginActivationException("wrapped", inner);

        Assert.IsFalse(string.IsNullOrEmpty(parameterless.Message));
        Assert.IsNull(parameterless.InnerException);
        Assert.AreEqual("activation failed", messageOnly.Message);
        Assert.IsNull(messageOnly.InnerException);
        Assert.AreEqual("wrapped", withInner.Message);
        Assert.AreSame(inner, withInner.InnerException);
    }

    /// <summary>
    /// Verifies that the parameterless and single-string constructors on
    /// <see cref="PluginMissingAttributeException" /> produce instances with the expected message and no inner
    /// exception, and that the message+inner constructor preserves both.
    /// </summary>
    [TestMethod]
    public void PluginMissingAttributeException_BasicCtors_ShouldExposeMessageAndInner()
    {
        var parameterless = new PluginMissingAttributeException();
        var messageOnly = new PluginMissingAttributeException("attribute missing");
        var inner = new InvalidOperationException("root");
        var withInner = new PluginMissingAttributeException("wrapped", inner);

        Assert.IsFalse(string.IsNullOrEmpty(parameterless.Message));
        Assert.IsNull(parameterless.InnerException);
        Assert.AreEqual("attribute missing", messageOnly.Message);
        Assert.IsNull(messageOnly.InnerException);
        Assert.AreEqual("wrapped", withInner.Message);
        Assert.AreSame(inner, withInner.InnerException);
    }

    /// <summary>
    /// Verifies that the parameterless and single-string constructors on
    /// <see cref="PluginNotTrustedException" /> produce instances with the expected message and no inner
    /// exception, and that the message+inner constructor preserves both.
    /// </summary>
    [TestMethod]
    public void PluginNotTrustedException_BasicCtors_ShouldExposeMessageAndInner()
    {
        var parameterless = new PluginNotTrustedException();
        var messageOnly = new PluginNotTrustedException("not trusted");
        var inner = new InvalidOperationException("root");
        var withInner = new PluginNotTrustedException("wrapped", inner);

        Assert.IsFalse(string.IsNullOrEmpty(parameterless.Message));
        Assert.IsNull(parameterless.InnerException);
        Assert.AreEqual("not trusted", messageOnly.Message);
        Assert.IsNull(messageOnly.InnerException);
        Assert.AreEqual("wrapped", withInner.Message);
        Assert.AreSame(inner, withInner.InnerException);
    }
}
