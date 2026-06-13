// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlNamingPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the parameter-validation contract shared by the built-in <see cref="TomlNamingPolicy" />
/// implementations: every policy rejects a <see langword="null" /> member name with
/// <see cref="ArgumentNullException" /> and <c>ParamName</c> <c>name</c>.
/// </summary>
[TestClass]
public class TomlNamingPolicyTests
{
    /// <summary>
    /// Verifies that <see cref="TomlNamingPolicy.CamelCase" /> throws <see cref="ArgumentNullException" /> with
    /// <c>ParamName</c> <c>name</c> when converting a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenNameIsNull_ForCamelCase_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = TomlNamingPolicy.CamelCase.ConvertName(null!);
        }, "name");
    }

    /// <summary>
    /// Verifies that <see cref="TomlNamingPolicy.SnakeCaseLower" /> throws <see cref="ArgumentNullException" />
    /// with <c>ParamName</c> <c>name</c> when converting a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenNameIsNull_ForSnakeCaseLower_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = TomlNamingPolicy.SnakeCaseLower.ConvertName(null!);
        }, "name");
    }

    /// <summary>
    /// Verifies that <see cref="TomlNamingPolicy.SnakeCaseUpper" /> throws <see cref="ArgumentNullException" />
    /// with <c>ParamName</c> <c>name</c> when converting a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenNameIsNull_ForSnakeCaseUpper_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = TomlNamingPolicy.SnakeCaseUpper.ConvertName(null!);
        }, "name");
    }

    /// <summary>
    /// Verifies that <see cref="TomlNamingPolicy.KebabCaseLower" /> throws <see cref="ArgumentNullException" />
    /// with <c>ParamName</c> <c>name</c> when converting a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenNameIsNull_ForKebabCaseLower_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = TomlNamingPolicy.KebabCaseLower.ConvertName(null!);
        }, "name");
    }

    /// <summary>
    /// Verifies that <see cref="TomlNamingPolicy.KebabCaseUpper" /> throws <see cref="ArgumentNullException" />
    /// with <c>ParamName</c> <c>name</c> when converting a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenNameIsNull_ForKebabCaseUpper_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = TomlNamingPolicy.KebabCaseUpper.ConvertName(null!);
        }, "name");
    }
}
