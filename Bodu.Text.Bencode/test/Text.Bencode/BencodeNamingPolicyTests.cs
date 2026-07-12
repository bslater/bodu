// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NamingPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Text.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the parameter-validation contract shared by the built-in <see cref="NamingPolicy" />
/// implementations: every policy rejects a <see langword="null" /> member name with
/// <see cref="ArgumentNullException" /> and <c>ParamName</c> <c>name</c>.
/// </summary>
[TestClass]
public class NamingPolicyTests
{
    /// <summary>
    /// Verifies that <see cref="NamingPolicy.CamelCase" /> throws <see cref="ArgumentNullException" /> with
    /// <c>ParamName</c> <c>name</c> when converting a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenNameIsNull_ForCamelCase_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = NamingPolicy.CamelCase.ConvertName(null!);
        }, "name");
    }

    /// <summary>
    /// Verifies that <see cref="NamingPolicy.SnakeCaseLower" /> throws <see cref="ArgumentNullException" />
    /// with <c>ParamName</c> <c>name</c> when converting a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenNameIsNull_ForSnakeCaseLower_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = NamingPolicy.SnakeCaseLower.ConvertName(null!);
        }, "name");
    }

    /// <summary>
    /// Verifies that <see cref="NamingPolicy.SnakeCaseUpper" /> throws <see cref="ArgumentNullException" />
    /// with <c>ParamName</c> <c>name</c> when converting a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenNameIsNull_ForSnakeCaseUpper_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = NamingPolicy.SnakeCaseUpper.ConvertName(null!);
        }, "name");
    }

    /// <summary>
    /// Verifies that <see cref="NamingPolicy.KebabCaseLower" /> throws <see cref="ArgumentNullException" />
    /// with <c>ParamName</c> <c>name</c> when converting a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenNameIsNull_ForKebabCaseLower_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = NamingPolicy.KebabCaseLower.ConvertName(null!);
        }, "name");
    }

    /// <summary>
    /// Verifies that <see cref="NamingPolicy.KebabCaseUpper" /> throws <see cref="ArgumentNullException" />
    /// with <c>ParamName</c> <c>name</c> when converting a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenNameIsNull_ForKebabCaseUpper_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = NamingPolicy.KebabCaseUpper.ConvertName(null!);
        }, "name");
    }
}
