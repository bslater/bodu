// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocConfigExceptionTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Bodu.CodeStyle.XmlDocumentation.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Configuration;

[TestClass]
public sealed class XmlDocConfigExceptionTests
{
    /// <summary>
    /// Verifies that the parameterless constructor returns a non-null exception.
    /// </summary>
    [TestMethod]
    public void Ctor_Default_ShouldReturnNonNullInstance()
    {
        var ex = new XmlDocConfigException();

        Assert.IsNotNull(ex);
    }

    /// <summary>
    /// Verifies that the message constructor surfaces the given message.
    /// </summary>
    [TestMethod]
    public void Ctor_WithMessage_ShouldExposeMessage()
    {
        var ex = new XmlDocConfigException("boom");

        Assert.AreEqual("boom", ex.Message);
    }

    /// <summary>
    /// Verifies that the inner-exception constructor surfaces both the message and the inner exception.
    /// </summary>
    [TestMethod]
    public void Ctor_WithMessageAndInner_ShouldExposeBoth()
    {
        var inner = new InvalidOperationException("root");

        var ex = new XmlDocConfigException("boom", inner);

        Assert.AreEqual("boom", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
    }
}
