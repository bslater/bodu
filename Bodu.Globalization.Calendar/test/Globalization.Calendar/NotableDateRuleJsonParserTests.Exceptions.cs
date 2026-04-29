// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.Exceptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Globalization.Calendar;

public partial class NotableDateRuleJsonParserTests
{
	/// <summary>
	/// Verifies that <see cref="NotableDateRuleJsonParser.ParseJson(string)" /> throws when given a null payload.
	/// </summary>
	[TestMethod]
	public void ParseJson_WhenInputIsNull_ShouldThrowArgumentNullException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			_ = NotableDateRuleJsonParser.ParseJson(null!);
		});
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateRuleJsonParser.ParseJson(string)" /> throws when given a whitespace payload.
	/// </summary>
	[TestMethod]
	public void ParseJson_WhenInputIsWhitespace_ShouldThrowArgumentNullException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			_ = NotableDateRuleJsonParser.ParseJson("   ");
		});
	}

	/// <summary>
	/// Verifies that malformed JSON surfaces as a <see cref="JsonException" /> from
	/// <see cref="System.Text.Json.JsonSerializer" />.
	/// </summary>
	[TestMethod]
	public void ParseJson_WhenInputIsMalformedJson_ShouldThrowJsonException()
	{
		Assert.ThrowsExactly<JsonException>(() =>
		{
			_ = NotableDateRuleJsonParser.ParseJson("{ this is not json }");
		});
	}

	/// <summary>
	/// Verifies that a rule declaring no strategy at all surfaces as
	/// <see cref="InvalidOperationException" />.
	/// </summary>
	[TestMethod]
	public void ParseJson_WhenRuleHasNoStrategy_ShouldThrowInvalidOperationException()
	{
		const string json = @"{
			""notableDates"": [
				{ ""name"": ""No Strategy"", ""rules"": [ { ""name"": ""Bad"", ""category"": ""Holiday"" } ] }
			]
		}";

		Assert.ThrowsExactly<InvalidOperationException>(() =>
		{
			_ = NotableDateRuleJsonParser.ParseJson(json);
		});
	}

	/// <summary>
	/// Verifies that a rule declaring more than one strategy surfaces as
	/// <see cref="InvalidOperationException" />.
	/// </summary>
	[TestMethod]
	public void ParseJson_WhenRuleHasMultipleStrategies_ShouldThrowInvalidOperationException()
	{
		const string json = @"{
			""notableDates"": [
				{ ""name"": ""Multiple"", ""rules"": [ {
					""name"": ""Bad"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""January"", ""day"": 1 },
					""algorithm"": { ""key"": ""whatever"" }
				} ] }
			]
		}";

		Assert.ThrowsExactly<InvalidOperationException>(() =>
		{
			_ = NotableDateRuleJsonParser.ParseJson(json);
		});
	}

	/// <summary>
	/// Verifies that two adjustments sharing the same key on a single rule surface as
	/// <see cref="InvalidOperationException" />.
	/// </summary>
	[TestMethod]
	public void ParseJson_WhenAdjustmentKeysDuplicate_ShouldThrowInvalidOperationException()
	{
		const string json = @"{
			""notableDates"": [
				{ ""name"": ""Dup"", ""rules"": [ {
					""name"": ""Bad"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""January"", ""day"": 1 },
					""adjustments"": [
						{ ""key"": ""same"", ""when"": ""IfWeekend"", ""action"": ""MoveToNextWeekday"" },
						{ ""key"": ""same"", ""when"": ""Always"", ""action"": ""None"" }
					]
				} ] }
			]
		}";

		Assert.ThrowsExactly<InvalidOperationException>(() =>
		{
			_ = NotableDateRuleJsonParser.ParseJson(json);
		});
	}

	/// <summary>
	/// Verifies that an unrecognised category enum value surfaces as
	/// <see cref="InvalidOperationException" />.
	/// </summary>
	[TestMethod]
	public void ParseJson_WhenCategoryIsUnknown_ShouldThrowInvalidOperationException()
	{
		const string json = @"{
			""notableDates"": [
				{ ""name"": ""Bogus"", ""rules"": [ {
					""name"": ""Bad"",
					""category"": ""SomethingMadeUp"",
					""fixed"": { ""month"": ""January"", ""day"": 1 }
				} ] }
			]
		}";

		Assert.ThrowsExactly<InvalidOperationException>(() =>
		{
			_ = NotableDateRuleJsonParser.ParseJson(json);
		});
	}

	/// <summary>
	/// Verifies that an unrecognised month token on a Fixed rule surfaces as
	/// <see cref="FormatException" />.
	/// </summary>
	[TestMethod]
	public void ParseJson_WhenFixedMonthIsUnknown_ShouldThrowFormatException()
	{
		const string json = @"{
			""notableDates"": [
				{ ""name"": ""Bogus"", ""rules"": [ {
					""name"": ""Bad"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""Smarch"", ""day"": 1 }
				} ] }
			]
		}";

		Assert.ThrowsExactly<FormatException>(() =>
		{
			_ = NotableDateRuleJsonParser.ParseJson(json);
		});
	}

	/// <summary>
	/// Verifies that omitting the required <c>month</c> field on a Fixed strategy surfaces as
	/// <see cref="InvalidOperationException" />.
	/// </summary>
	[TestMethod]
	public void ParseJson_WhenFixedMissingMonth_ShouldThrowInvalidOperationException()
	{
		const string json = @"{
			""notableDates"": [
				{ ""name"": ""Bogus"", ""rules"": [ {
					""name"": ""Bad"",
					""category"": ""Holiday"",
					""fixed"": { ""day"": 1 }
				} ] }
			]
		}";

		Assert.ThrowsExactly<InvalidOperationException>(() =>
		{
			_ = NotableDateRuleJsonParser.ParseJson(json);
		});
	}
}
