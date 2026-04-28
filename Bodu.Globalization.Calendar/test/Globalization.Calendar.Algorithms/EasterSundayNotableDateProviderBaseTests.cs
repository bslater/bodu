// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EasterSundayNotableDateProviderBaseTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using SysGlob = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the shared contract implemented in <see cref="EasterSundayNotableDateProviderBase" />
/// via a minimal test-double subclass.
/// </summary>
[TestClass]
public sealed class EasterSundayNotableDateProviderBaseTests
{
	/// <summary>
	/// Verifies that the default <see cref="EasterSundayNotableDateProviderBase.MaxSupportedYear" />
	/// is 9999 unless overridden.
	/// </summary>
	[TestMethod]
	public void MaxSupportedYear_WhenNotOverridden_ShouldDefaultTo9999()
	{
		var provider = new DefaultTestProvider();

		Assert.AreEqual(9999, provider.MaxSupportedYear);
	}

	/// <summary>
	/// Verifies that the default tag set contains the expected Christianity/Easter entries.
	/// </summary>
	[TestMethod]
	public void DefaultTags_ShouldIncludeChristianityAndEaster()
	{
		var provider = new DefaultTestProvider();

		IReadOnlyList<NotableDate> result = provider.GetDates(2026);

		Assert.IsTrue(result[0].Tags.Contains("Christianity"));
		Assert.IsTrue(result[0].Tags.Contains("Easter"));
	}

	/// <summary>
	/// Verifies that the emitted <see cref="NotableDate" /> pulls metadata from the subclass
	/// overrides (name, category, comment, tags).
	/// </summary>
	[TestMethod]
	public void GetDates_WhenSubclassOverridesMetadata_ShouldForwardAllMetadataOntoNotableDate()
	{
		var provider = new CustomMetadataTestProvider();

		NotableDate notable = provider.GetDates(2026)[0];

		Assert.AreEqual("Custom Name", notable.Name);
		Assert.AreEqual(NotableDateCategory.Observance, notable.Category);
		Assert.AreEqual("custom comment", notable.Comment);
		CollectionAssert.AreEquivalent(new[] { "CustomTag" }, notable.Tags.ToArray());
		Assert.AreEqual(1, notable.DurationDays);
		Assert.IsFalse(notable.IsNonWorkingDay);
	}

	/// <summary>
	/// Verifies that a subclass that throws from <c>ValidateCalendar</c> surfaces the exception
	/// to the caller and never invokes <c>CalculateDate</c>.
	/// </summary>
	[TestMethod]
	public void GetDates_WhenSubclassValidateCalendarRejects_ShouldThrowWithoutCalculating()
	{
		var provider = new RejectingTestProvider();

		Assert.ThrowsExactly<NotSupportedException>(() =>
		{
			_ = provider.GetDates(2026, new SysGlob.JulianCalendar());
		});

		Assert.AreEqual(0, provider.CalculateDateCallCount);
	}

	/// <summary>
	/// Verifies that repeated calls for the same year use the per-year cache and do not invoke
	/// <c>CalculateDate</c> a second time.
	/// </summary>
	[TestMethod]
	public void GetDates_WhenCalledTwiceForSameYear_ShouldReuseCachedCalculation()
	{
		var provider = new CountingTestProvider();

		_ = provider.GetDates(2026);
		_ = provider.GetDates(2026);

		Assert.AreEqual(1, provider.CalculateDateCallCount);
	}

	/// <summary>
	/// Verifies that the null-calendar path falls back to the subclass-declared
	/// <c>DefaultCalendarType</c> and that the non-null path uses the caller-supplied type.
	/// </summary>
	[TestMethod]
	public void GetDates_WhenCalendarIsNull_ShouldUseDefaultCalendarType_AndUseSuppliedWhenNotNull()
	{
		var provider = new CustomMetadataTestProvider();

		NotableDate nullCalendar = provider.GetDates(2026)[0];
		NotableDate gregorianCalendar = provider.GetDates(2027, new SysGlob.GregorianCalendar())[0];

		Assert.AreEqual(typeof(SysGlob.JulianCalendar), nullCalendar.CalendarType);
		Assert.AreEqual(typeof(SysGlob.GregorianCalendar), gregorianCalendar.CalendarType);
	}

	/// <summary>
	/// Test double that lets every calendar through and uses a fixed date for calculation.
	/// </summary>
	private sealed class DefaultTestProvider : EasterSundayNotableDateProviderBase
	{
		public override int MinSupportedYear => 1;

		protected override string Name => "Test Easter";

		protected override NotableDateCategory Category => NotableDateCategory.Cultural;

		protected override void ValidateCalendar(SysGlob.Calendar? calendar)
		{
			// Accept any calendar.
		}

		protected override DateTime CalculateDate(int year) => new DateTime(year, 4, 5, 0, 0, 0, DateTimeKind.Unspecified);
	}

	/// <summary>
	/// Test double exercising the overridable metadata hooks.
	/// </summary>
	private sealed class CustomMetadataTestProvider : EasterSundayNotableDateProviderBase
	{
		public override int MinSupportedYear => 1;

		protected override string Name => "Custom Name";

		protected override NotableDateCategory Category => NotableDateCategory.Observance;

		protected override string? Comment => "custom comment";

		protected override ImmutableHashSet<string> Tags =>
			ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "CustomTag");

		protected override Type? DefaultCalendarType => typeof(SysGlob.JulianCalendar);

		protected override void ValidateCalendar(SysGlob.Calendar? calendar)
		{
			// Accept any calendar.
		}

		protected override DateTime CalculateDate(int year) => new DateTime(year, 4, 5, 0, 0, 0, DateTimeKind.Unspecified);
	}

	/// <summary>
	/// Test double that rejects every non-null calendar and counts calls into
	/// <c>CalculateDate</c> so tests can confirm short-circuit behaviour.
	/// </summary>
	private sealed class RejectingTestProvider : EasterSundayNotableDateProviderBase
	{
		public override int MinSupportedYear => 1;

		public int CalculateDateCallCount { get; private set; }

		protected override string Name => "Rejecting";

		protected override NotableDateCategory Category => NotableDateCategory.Cultural;

		protected override void ValidateCalendar(SysGlob.Calendar? calendar)
		{
			if (calendar is not null)
				throw new NotSupportedException("calendar not supported");
		}

		protected override DateTime CalculateDate(int year)
		{
			CalculateDateCallCount++;
			return new DateTime(year, 4, 5, 0, 0, 0, DateTimeKind.Unspecified);
		}
	}

	/// <summary>
	/// Test double that counts invocations of <c>CalculateDate</c> so tests can confirm the
	/// base-class cache is being used.
	/// </summary>
	private sealed class CountingTestProvider : EasterSundayNotableDateProviderBase
	{
		public override int MinSupportedYear => 1;

		public int CalculateDateCallCount { get; private set; }

		protected override string Name => "Counting";

		protected override NotableDateCategory Category => NotableDateCategory.Cultural;

		protected override void ValidateCalendar(SysGlob.Calendar? calendar)
		{
			// Accept any calendar.
		}

		protected override DateTime CalculateDate(int year)
		{
			CalculateDateCallCount++;
			return new DateTime(year, 4, 5, 0, 0, 0, DateTimeKind.Unspecified);
		}
	}
}
