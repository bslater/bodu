// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundExceptionHierarchyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies that every compound-file exception derives from the shared <see cref="CompoundFileException" /> base.
/// </summary>
[TestClass]
public partial class CompoundExceptionHierarchyTests
{
    /// <summary>
    /// Verifies that <see cref="CompoundFileFormatException" /> derives from <see cref="CompoundFileException" />.
    /// </summary>
    [TestMethod]
    public void CompoundFileFormatException_ShouldDeriveFromCompoundFileException()
    {
        Assert.IsInstanceOfType<CompoundFileException>(new CompoundFileFormatException("x"));
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFileSerializationException" /> derives from <see cref="CompoundFileException" />.
    /// </summary>
    [TestMethod]
    public void CompoundFileSerializationException_ShouldDeriveFromCompoundFileException()
    {
        Assert.IsInstanceOfType<CompoundFileException>(new CompoundFileSerializationException("x"));
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStreamNotFoundException" /> derives from <see cref="CompoundFileException" />.
    /// </summary>
    [TestMethod]
    public void CompoundStreamNotFoundException_ShouldDeriveFromCompoundFileException()
    {
        Assert.IsInstanceOfType<CompoundFileException>(new CompoundStreamNotFoundException("x"));
    }

    /// <summary>
    /// Verifies that a malformed container's exception can be caught through the shared base type.
    /// </summary>
    [TestMethod]
    public void Open_WhenSignatureInvalid_ShouldBeCatchableAsCompoundFileException()
    {
        CompoundFileException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var _ = CompoundFile.Open(new MemoryStream(new byte[512]));
        });

        Assert.IsInstanceOfType<CompoundFileException>(ex);
    }
}
