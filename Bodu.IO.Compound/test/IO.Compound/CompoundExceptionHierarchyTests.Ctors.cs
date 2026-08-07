// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundExceptionHierarchyTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies the standard constructor set on every compound-file exception type.
/// </summary>
/// <remarks>
/// The library throws only through the message constructors, so the parameterless and message-plus-inner overloads
/// exist for callers who rethrow or wrap. They are part of the public surface, and a constructor that forgets to chain
/// to its base silently drops the message or the inner exception - which is exactly what these assert against.
/// </remarks>
public partial class CompoundExceptionHierarchyTests
{
    /// <summary>
    /// Gets the exception types that carry the standard three-constructor set, paired with factories for each
    /// overload.
    /// </summary>
    /// <value>Rows of type name, parameterless factory, message factory, and message-plus-inner factory.</value>
    public static IEnumerable<object[]> ExceptionTypes =>
    [
        [
            nameof(CompoundFileException),
            (Func<Exception>)(() => new CompoundFileException()),
            (Func<string, Exception>)(m => new CompoundFileException(m)),
            (Func<string, Exception, Exception>)((m, i) => new CompoundFileException(m, i)),
        ],
        [
            nameof(CompoundFileFormatException),
            (Func<Exception>)(() => new CompoundFileFormatException()),
            (Func<string, Exception>)(m => new CompoundFileFormatException(m)),
            (Func<string, Exception, Exception>)((m, i) => new CompoundFileFormatException(m, i)),
        ],
        [
            nameof(CompoundFileSerializationException),
            (Func<Exception>)(() => new CompoundFileSerializationException()),
            (Func<string, Exception>)(m => new CompoundFileSerializationException(m)),
            (Func<string, Exception, Exception>)((m, i) => new CompoundFileSerializationException(m, i)),
        ],
        [
            nameof(CompoundStreamNotFoundException),
            (Func<Exception>)(() => new CompoundStreamNotFoundException()),
            (Func<string, Exception>)(m => new CompoundStreamNotFoundException(m)),
            (Func<string, Exception, Exception>)((m, i) => new CompoundStreamNotFoundException(m, i)),
        ],
    ];

    /// <summary>
    /// Verifies that the parameterless constructor produces an exception with a non-empty framework-supplied message
    /// and no inner exception.
    /// </summary>
    /// <param name="typeName">The exception type name, for the failure label.</param>
    /// <param name="parameterless">Creates the exception through its parameterless constructor.</param>
    /// <param name="withMessage">Unused by this test.</param>
    /// <param name="withInner">Unused by this test.</param>
    [TestMethod]
    [DynamicData(nameof(ExceptionTypes))]
    public void Ctor_WhenParameterless_ShouldProduceDefaultMessageAndNoInner(
        string typeName,
        Func<Exception> parameterless,
        Func<string, Exception> withMessage,
        Func<string, Exception, Exception> withInner)
    {
        _ = withMessage;
        _ = withInner;

        Exception ex = parameterless();

        Assert.IsNotEmpty(ex.Message, typeName);
        Assert.IsNull(ex.InnerException, typeName);
    }

    /// <summary>
    /// Verifies that the message constructor surfaces the supplied message verbatim.
    /// </summary>
    /// <param name="typeName">The exception type name, for the failure label.</param>
    /// <param name="parameterless">Unused by this test.</param>
    /// <param name="withMessage">Creates the exception through its message constructor.</param>
    /// <param name="withInner">Unused by this test.</param>
    [TestMethod]
    [DynamicData(nameof(ExceptionTypes))]
    public void Ctor_WhenMessageSupplied_ShouldSurfaceItVerbatim(
        string typeName,
        Func<Exception> parameterless,
        Func<string, Exception> withMessage,
        Func<string, Exception, Exception> withInner)
    {
        _ = parameterless;
        _ = withInner;

        Exception ex = withMessage("the message");

        Assert.AreEqual("the message", ex.Message, typeName);
        Assert.IsNull(ex.InnerException, typeName);
    }

    /// <summary>
    /// Verifies that the message-plus-inner constructor chains both to the base, so neither the message nor the cause
    /// is dropped.
    /// </summary>
    /// <param name="typeName">The exception type name, for the failure label.</param>
    /// <param name="parameterless">Unused by this test.</param>
    /// <param name="withMessage">Unused by this test.</param>
    /// <param name="withInner">Creates the exception through its message-plus-inner constructor.</param>
    [TestMethod]
    [DynamicData(nameof(ExceptionTypes))]
    public void Ctor_WhenInnerExceptionSupplied_ShouldChainBothToBase(
        string typeName,
        Func<Exception> parameterless,
        Func<string, Exception> withMessage,
        Func<string, Exception, Exception> withInner)
    {
        _ = parameterless;
        _ = withMessage;

        var cause = new InvalidOperationException("cause");

        Exception ex = withInner("the message", cause);

        Assert.AreEqual("the message", ex.Message, typeName);
        Assert.AreSame(cause, ex.InnerException, typeName);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFileFormatException.Category" /> defaults to
    /// <see cref="CompoundFileError.Unknown" /> on the constructors that do not take one, and carries the supplied
    /// value on the one that does.
    /// </summary>
    [TestMethod]
    public void Category_WhenNotSupplied_ShouldDefaultToUnknown()
    {
        Assert.AreEqual(CompoundFileError.Unknown, new CompoundFileFormatException().Category);
        Assert.AreEqual(CompoundFileError.Unknown, new CompoundFileFormatException("x").Category);
        Assert.AreEqual(
            CompoundFileError.FatCycle,
            new CompoundFileFormatException("x", CompoundFileError.FatCycle).Category);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStreamNotFoundException.StreamName" /> is <see langword="null" /> unless the
    /// instance was created for a named stream, so a caller reading it can tell "unknown" from a real name.
    /// </summary>
    [TestMethod]
    public void StreamName_WhenConstructedWithoutAName_ShouldBeNull()
    {
        Assert.IsNull(new CompoundStreamNotFoundException().StreamName);
        Assert.IsNull(new CompoundStreamNotFoundException("x").StreamName);
        Assert.IsNull(new CompoundStreamNotFoundException("x", new InvalidOperationException()).StreamName);
    }
}
