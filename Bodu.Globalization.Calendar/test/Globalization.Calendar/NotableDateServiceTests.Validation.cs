// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.Validation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the strict rule-set validation surface (F-015): the <see cref="NotableDateService.Validate" /> inspection
/// method and the eager <see cref="NotableDateServiceOptions.ValidateRules" /> constructor check.
/// </summary>
public sealed partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that an offset rule whose anchor does not exist is reported as a <c>MissingAnchor</c> error.
    /// </summary>
    [TestMethod]
    public void Validate_WhenOffsetAnchorMissing_ShouldReportError()
    {
        NotableDateService service = BuildService(DanglingOffsetRule(), Fixed("Sanity", 1, 1));

        IReadOnlyList<NotableDateValidationDiagnostic> diagnostics = service.Validate();

        Assert.IsTrue(diagnostics.Any(d => d.Severity == NotableDateValidationSeverity.Error && d.Code == "MissingAnchor"));
    }

    /// <summary>
    /// Verifies that a clean rule set produces no error diagnostics.
    /// </summary>
    [TestMethod]
    public void Validate_WhenRuleSetIsValid_ShouldReportNoErrors()
    {
        NotableDateService service = BuildService(Fixed("Clean", 1, 1), Fixed("Also Clean", 12, 25));

        Assert.IsFalse(service.Validate().Any(d => d.Severity == NotableDateValidationSeverity.Error));
    }

    /// <summary>
    /// Verifies that an algorithm rule whose key is not registered is reported as an <c>UnregisteredAlgorithm</c>
    /// warning (optional algorithm packs may supply it later).
    /// </summary>
    [TestMethod]
    public void Validate_WhenAlgorithmKeyUnregistered_ShouldReportWarning()
    {
        NotableDateRule algo = new()
        {
            Name = "Algo",
            Strategy = DateResolutionStrategy.Algorithm,
            Category = NotableDateCategory.Religious,
            AlgorithmKey = "not-registered",
        };
        NotableDateService service = BuildService(algo);

        Assert.IsTrue(service.Validate().Any(d => d.Severity == NotableDateValidationSeverity.Warning && d.Code == "UnregisteredAlgorithm"));
    }

    /// <summary>
    /// Verifies that constructing a service with <see cref="NotableDateServiceOptions.ValidateRules" /> set throws when
    /// the rule set contains a validation error.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValidateRulesAndAnchorMissing_ShouldThrowExactlyInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new NotableDateService(
                [(INotableDateRuleProvider)new InMemoryRuleProvider(DanglingOffsetRule())],
                WorkingDaysOfWeek.MondayToFriday,
                new NotableDateServiceOptions { ValidateRules = true });
        });
    }

    /// <summary>
    /// Verifies that constructing a service with <see cref="NotableDateServiceOptions.ValidateRules" /> set succeeds for
    /// a valid rule set.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValidateRulesAndRuleSetValid_ShouldNotThrow()
    {
        NotableDateService service = new(
            [(INotableDateRuleProvider)new InMemoryRuleProvider(Fixed("Clean", 1, 1))],
            WorkingDaysOfWeek.MondayToFriday,
            new NotableDateServiceOptions { ValidateRules = true });

        Assert.IsNotNull(service);
    }

    /// <summary>
    /// Creates an offset rule whose anchor name does not resolve to any rule.
    /// </summary>
    /// <returns>The dangling offset rule.</returns>
    private static NotableDateRule DanglingOffsetRule() =>
        new()
        {
            Name = "Dangler",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Observance,
            AnchorRuleName = "DoesNotExist",
            OffsetDays = 1,
        };
}
