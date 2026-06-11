// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlNodeTests.Conversions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Verifies the <see cref="TomlNode" /> conversion and navigation surface: the implicit wrapping and explicit reading
/// operators across all eight scalar kinds, and the node-level indexers that delegate to the concrete container
/// kinds.
/// </summary>
public partial class TomlNodeTests
{
    /// <summary>
    /// Verifies that the implicit operators wrap each supported scalar into a <see cref="TomlValue" /> of the
    /// matching kind.
    /// </summary>
    [TestMethod]
    public void ImplicitOperators_WhenWrappingScalars_ShouldCreateValuesOfMatchingKind()
    {
        TomlNode fromString = "text";
        TomlNode fromInt64 = 42L;
        TomlNode fromInt32 = 7;
        TomlNode fromDouble = 1.5d;
        TomlNode fromBoolean = true;
        TomlNode fromOffsetDateTime = new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero);
        TomlNode fromDateTime = new DateTime(1979, 5, 27, 7, 32, 0);
        TomlNode fromDateOnly = new DateOnly(1979, 5, 27);
        TomlNode fromTimeOnly = new TimeOnly(7, 32, 0);

        Assert.AreEqual(TomlValueKind.String, fromString.GetValueKind());
        Assert.AreEqual(TomlValueKind.Integer, fromInt64.GetValueKind());
        Assert.AreEqual(7L, fromInt32.GetValue<long>());
        Assert.AreEqual(TomlValueKind.Float, fromDouble.GetValueKind());
        Assert.AreEqual(TomlValueKind.Boolean, fromBoolean.GetValueKind());
        Assert.AreEqual(TomlValueKind.OffsetDateTime, fromOffsetDateTime.GetValueKind());
        Assert.AreEqual(TomlValueKind.LocalDateTime, fromDateTime.GetValueKind());
        Assert.AreEqual(TomlValueKind.LocalDate, fromDateOnly.GetValueKind());
        Assert.AreEqual(TomlValueKind.LocalTime, fromTimeOnly.GetValueKind());
    }

    /// <summary>
    /// Verifies that the explicit reading operators return the wrapped value for each date-time kind.
    /// </summary>
    [TestMethod]
    public void ExplicitOperators_WhenReadingDateTimeKinds_ShouldReturnUnderlyingValues()
    {
        var odt = new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.FromHours(2));
        var ldt = new DateTime(1979, 5, 27, 7, 32, 0);
        var ld = new DateOnly(1979, 5, 27);
        var lt = new TimeOnly(7, 32, 0);

        Assert.AreEqual(odt, (DateTimeOffset)(TomlNode)TomlValue.Create(odt));
        Assert.AreEqual(ldt, (DateTime)(TomlNode)TomlValue.Create(ldt));
        Assert.AreEqual(ld, (DateOnly)(TomlNode)TomlValue.Create(ld));
        Assert.AreEqual(lt, (TimeOnly)(TomlNode)TomlValue.Create(lt));
    }

    /// <summary>
    /// Verifies that the explicit reading operator for <see cref="double" /> returns the wrapped float value.
    /// </summary>
    [TestMethod]
    public void ExplicitOperators_WhenReadingFloat_ShouldReturnUnderlyingValue()
    {
        Assert.AreEqual(1.5d, (double)(TomlNode)TomlValue.Create(1.5d));
    }

    /// <summary>
    /// Verifies that each explicit reading operator throws <see cref="ArgumentNullException" /> with
    /// <c>ParamName</c> <c>node</c> when the node is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ExplicitOperators_WhenNodeIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = (string)(TomlNode)null!;
        }, "node");

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = (long)(TomlNode)null!;
        }, "node");

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = (int)(TomlNode)null!;
        }, "node");

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = (double)(TomlNode)null!;
        }, "node");

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = (bool)(TomlNode)null!;
        }, "node");

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = (DateTimeOffset)(TomlNode)null!;
        }, "node");

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = (DateTime)(TomlNode)null!;
        }, "node");

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = (DateOnly)(TomlNode)null!;
        }, "node");

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = (TimeOnly)(TomlNode)null!;
        }, "node");
    }

    /// <summary>
    /// Verifies that the explicit reading operators throw <see cref="InvalidOperationException" /> when the node's
    /// kind does not match the requested type.
    /// </summary>
    [TestMethod]
    public void ExplicitOperators_WhenKindMismatched_ShouldThrowInvalidOperationException()
    {
        TomlNode integer = TomlValue.Create(1L);
        TomlNode text = TomlValue.Create("x");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = (string)integer;
        });

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = (long)text;
        });

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = (DateTimeOffset)integer;
        });
    }

    /// <summary>
    /// Verifies that the node-level positional indexer throws <see cref="InvalidOperationException" /> when the node
    /// is not an array.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIntIndexerUsedOnObject_ShouldThrowInvalidOperationException()
    {
        TomlNode node = new TomlObject();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = node[0];
        });
    }

    /// <summary>
    /// Verifies that the node-level named indexer throws <see cref="InvalidOperationException" /> when the node is
    /// not a table.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenStringIndexerUsedOnArray_ShouldThrowInvalidOperationException()
    {
        TomlNode node = new TomlArray();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = node["key"];
        });
    }

    /// <summary>
    /// Verifies that the node-level indexers delegate get and set access to the concrete container kinds.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenUsedOnMatchingKinds_ShouldDelegateToContainer()
    {
        TomlNode objectNode = new TomlObject();
        TomlNode arrayNode = new TomlArray(0);

        objectNode["k"] = 1L;
        arrayNode[0] = "x";

        Assert.AreEqual(1L, (long)objectNode["k"]!);
        Assert.AreEqual("x", (string)arrayNode[0]!);
    }

    /// <summary>
    /// Verifies that <see cref="TomlNode.GetValue{T}" /> throws <see cref="InvalidOperationException" /> when the
    /// node is a container rather than a scalar value.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenNodeIsContainer_ShouldThrowInvalidOperationException()
    {
        TomlNode node = new TomlObject();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = node.GetValue<long>();
        });
    }

    /// <summary>
    /// Verifies that a node without a container reports a <see langword="null" /> <see cref="TomlNode.Parent" /> and
    /// returns itself from <see cref="TomlNode.Root" />.
    /// </summary>
    [TestMethod]
    public void Root_WhenNodeHasNoParent_ShouldReturnSelf()
    {
        TomlValue node = TomlValue.Create(1L);

        Assert.IsNull(node.Parent);
        Assert.AreSame(node, node.Root);
    }
}
