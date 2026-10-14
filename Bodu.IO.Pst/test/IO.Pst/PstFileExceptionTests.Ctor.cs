// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileExceptionTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.IO.Pst;

public partial class PstFileExceptionTests
{
    /// <summary>
    /// Verifies that the base exception no longer declares the <c>(message, innerException, error)</c> constructor, which
    /// took the same arguments as <c>(message, error, innerException)</c> in a different order.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInnerExceptionPrecedesError_ShouldNotBeDeclared()
    {
        ConstructorInfo? constructor = typeof(PstFileException).GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(string), typeof(Exception), typeof(PstFileError)]);

        Assert.IsNull(constructor);
    }

    /// <summary>
    /// Verifies that the base exception declares exactly one public constructor per argument set: the three standard
    /// constructors, then the two that take an error category, with the inner exception last.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPublicConstructorsAreEnumerated_ShouldDeclareOnePerArgumentSet()
    {
        string[] expected =
        [
            "()",
            "(String)",
            "(String, Exception)",
            "(String, PstFileError)",
            "(String, PstFileError, Exception)",
        ];

        string[] actual = typeof(PstFileException)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Select(static c => "(" + string.Join(", ", c.GetParameters().Select(static p => p.ParameterType.Name)) + ")")
            .Order(StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(expected.Order(StringComparer.Ordinal).ToArray(), actual);
    }

    /// <summary>
    /// Verifies that the <c>(message, error, innerException)</c> constructor of the base and of the format exception
    /// preserves the message, the error category, and the inner exception.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageErrorAndInner_ShouldPreserveAll()
    {
        var inner = new InvalidOperationException("cause");

        foreach (PstFileException ex in new PstFileException[]
        {
            new("boom", PstFileError.InvalidBlock, inner),
            new PstFileFormatException("boom", PstFileError.InvalidBlock, inner),
        })
        {
            Assert.AreEqual("boom", ex.Message);
            Assert.AreEqual(PstFileError.InvalidBlock, ex.Error);
            Assert.AreSame(inner, ex.InnerException);
        }
    }

    /// <summary>
    /// Verifies that the message-and-inner constructor of each fixed-category exception preserves the inner exception
    /// and records the type's own error category.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageAndInner_ForFixedCategoryTypes_ShouldPreserveInnerAndCategory()
    {
        var inner = new InvalidOperationException("cause");

        var unsupported = new PstUnsupportedFormatException("boom", inner);
        var notFound = new PstNodeNotFoundException("boom", inner);

        Assert.AreSame(inner, unsupported.InnerException);
        Assert.AreEqual(PstFileError.UnsupportedFormat, unsupported.Error);
        Assert.AreSame(inner, notFound.InnerException);
        Assert.AreEqual(PstFileError.NodeNotFound, notFound.Error);
    }
}
